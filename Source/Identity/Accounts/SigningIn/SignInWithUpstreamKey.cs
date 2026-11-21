// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;
using Cratis.Monads;

namespace Cratis.Identity.Accounts.SigningIn;

/// <summary>
/// Signs in only with observations supplied by the trusted protocol adapter.
/// </summary>
/// <param name="OperationId">The identifier bound to the verified authentication.</param>
[Command, ProtocolCommand, ExcludeFromDiscovery, ProtectedDecision]
internal record SignInWithUpstreamKey(FactId OperationId)
{
    /// <summary>
    /// Protects the account source before waiting for and rechecking the materialized decision inputs.
    /// </summary>
    /// <param name="verified">The trusted request-scoped observations.</param>
    /// <param name="gate">The upstream authentication gate.</param>
    /// <param name="lookups">The reservation reader.</param>
    /// <param name="decisions">The command-owned optimistic concurrency guard.</param>
    /// <param name="accounts">The passive materialized account reader.</param>
    /// <param name="catchUp">The filtered projection progress fence.</param>
    /// <param name="timeProvider">The clock.</param>
    /// <returns>The guarded decision, or a refusal.</returns>
    public async Task<Result<Decision, ValidationResult>> Provide(
        IVerifiedUpstreamSignIn verified,
        VerifiedKeyGate gate,
        UpstreamLookups lookups,
        IDecisionReads decisions,
        AccountReader accounts,
        ReadModelCatchUp catchUp,
        TimeProvider timeProvider)
    {
        if (Validate(verified, timeProvider) is { } invalid)
        {
            return invalid;
        }

        var authentication = verified.Authentication!;
        var profile = verified.Profile!;
        var key = gate.KeyFor(authentication);
        if (gate.Validate(authentication, key) is { } refusal)
        {
            return refusal;
        }

        var hint = await lookups.Find(key.Digest);
        var subject = hint?.AccountSubject ?? AccountSubject.New();

        // This enrolled read survives until Arc commits the returned events. It protects every
        // account input from its observed sequence, including an unlink/disable after recheck.
        _ = await decisions.Get<AccountGuard>(subject.Value);
        await catchUp.BeforeRetry(verified.CancellationToken);
        var reservation = await lookups.Find(key.Digest);
        if (reservation?.AccountSubject != hint?.AccountSubject || reservation?.LinkId != hint?.LinkId)
        {
            return ValidationResult.Error("subject_already_reserved");
        }

        // Confirm enrollment at the recheck. Arc caches the issued read for this invocation,
        // retaining its earlier boundary rather than weakening protection after catch-up.
        _ = await decisions.Get<AccountGuard>(subject.Value);
        var account = await accounts.Find(subject);
        return new Decision(subject, authentication, key.Digest, profile, reservation, account, timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Produces decision events without appending or creating a session.
    /// </summary>
    /// <param name="decision">The guarded, caught-up decision inputs.</param>
    /// <returns>The events and subject, or a refusal without events.</returns>
    public Result<(IEnumerable<EventForEventSourceId>, AccountSubject), ValidationResult> Handle(Decision decision)
    {
        if (decision.Reservation is null)
        {
            return decision.Account is null
                ? (Register(decision), decision.Subject)
                : ValidationResult.Error("subject_already_reserved");
        }

        if (decision.Account is { Status: not AccountStatus.Active })
        {
            return ValidationResult.Error("account_not_active");
        }

        var link = decision.Account?.LinkedUpstreamKeys.SingleOrDefault(link => link.LinkId == decision.Reservation.LinkId);
        if (!decision.Reservation.Active || link is null)
        {
            return ValidationResult.Error("upstream_key_reserved_but_inactive");
        }

        if (link.IdpIssuer != decision.Authentication.IdpIssuer || link.IdpSub != decision.Authentication.Subject)
        {
            return ValidationResult.Error("upstream_key_reserved_but_inactive");
        }

        return (ProfileChanges(decision, link), decision.Subject);
    }

    ValidationResult? Validate(IVerifiedUpstreamSignIn verified, TimeProvider timeProvider)
    {
        if (verified.Authentication is not { } authentication || verified.Profile is null)
        {
            return ValidationResult.Error("verified_sign_in_required");
        }

        if (authentication.OperationId != OperationId)
        {
            return ValidationResult.Error("operation_id_mismatch");
        }

        var age = timeProvider.GetUtcNow() - authentication.AuthTime;
        return age < TimeSpan.Zero || age > TimeSpan.FromMinutes(5)
            ? ValidationResult.Error("fresh_upstream_authentication_required")
            : null;
    }

    static IEnumerable<EventForEventSourceId> Register(Decision decision)
    {
        var authentication = decision.Authentication;
        LinkId link = FactId.ForOperation(authentication.OperationId, nameof(UpstreamKeyLinkRecorded)).Value;
        IEnumerable<object> events =
        [
            new AccountRegistrationRecorded(authentication.AuthTime, FactId.ForOperation(authentication.OperationId, nameof(AccountRegistrationRecorded))),
            new UpstreamKeyLinkRecorded(link, authentication.ConnectionId, authentication.IdpIssuer, decision.Digest, authentication.OperationId.Value, authentication.AuthTime, FactId.ForOperation(authentication.OperationId, nameof(UpstreamKeyLinkRecorded))),
            new UpstreamKeySubjectRecorded(link, authentication.IdpIssuer, authentication.Subject),
            new UpstreamKeyDisplayIdentifierObserved(link, decision.Profile.Login, decision.At),
            new AccountNameChanged(decision.Profile.Name)
        ];
        if (decision.Profile.Email is { } email)
        {
            events = events.Append(new AccountEmailChanged(email));
        }

        return events.Select(@event => new EventForEventSourceId(decision.Subject.Value, @event));
    }

    static IEnumerable<EventForEventSourceId> ProfileChanges(Decision decision, LinkedUpstreamKey link)
    {
        var events = new List<object>();
        if (link.Login != decision.Profile.Login)
        {
            events.Add(new UpstreamKeyDisplayIdentifierObserved(link.LinkId, decision.Profile.Login, decision.At));
        }

        if (decision.Profile.Email is { } email && decision.Account!.Email != email)
        {
            events.Add(new AccountEmailChanged(email));
        }

        if (decision.Account!.Name != decision.Profile.Name)
        {
            events.Add(new AccountNameChanged(decision.Profile.Name));
        }

        return events.Select(@event => new EventForEventSourceId(decision.Subject.Value, @event));
    }

    /// <summary>
    /// The trusted observations and guarded, caught-up account state for one decision attempt.
    /// </summary>
    /// <param name="Subject">The account event source.</param>
    /// <param name="Authentication">The verified authentication.</param>
    /// <param name="Digest">The exact upstream key pseudonym.</param>
    /// <param name="Profile">The observed profile.</param>
    /// <param name="Reservation">The permanent key reservation.</param>
    /// <param name="Account">The caught-up account.</param>
    /// <param name="At">The decision time.</param>
    internal record Decision(AccountSubject Subject, VerifiedAuthentication Authentication, UpstreamLookupDigest Digest, ObservedProfile Profile, UpstreamKeyMatch? Reservation, Account? Account, DateTimeOffset At);
}
