// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;
using Cratis.Monads;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.Linking.Proposing;

/// <summary>
/// Proposes an explicitly requested link using scope-local dual proof.
/// </summary>
[Command, ProtocolCommand, ExcludeFromDiscovery, ProtectedDecision]
internal record ProposeLink
{
    /// <summary>
    /// Verifies ceremony proof and enrolls the account guard before reading link state.
    /// </summary>
    /// <param name="ceremony">The trusted scope-local ceremony observations.</param>
    /// <param name="decisions">The command-owned optimistic concurrency guard.</param>
    /// <param name="accounts">The passive account link-state reader.</param>
    /// <param name="lookups">The permanent reservation hints.</param>
    /// <param name="digester">The immutable upstream key digester.</param>
    /// <param name="options">The realm hold policy.</param>
    /// <param name="time">The trusted clock.</param>
    /// <returns>The guarded decision inputs, or a refusal.</returns>
    public async Task<Result<Decision, ValidationResult>> Provide(ILinkCeremony ceremony, IDecisionReads decisions, AccountReader accounts, UpstreamLookups lookups, UpstreamKeyDigester digester, IOptions<IdentityOptions> options, TimeProvider time)
    {
        if (ceremony.Intent.EmailOnlyProof)
        {
            return ValidationResult.Error("email_is_not_identity_proof");
        }

        var session = LinkDecisions.RequireSession(ceremony.Session, time);
        if (session.TryGetError(out var refusal))
        {
            return refusal;
        }

        if (!ceremony.Intent.Explicit)
        {
            return ValidationResult.Error("explicit_dual_proof_required");
        }

        var existing = session.AsT0;
        if (LinkDecisions.RequireProof(ceremony.Authentication, existing.SessionId, time.GetUtcNow()) is { } proofRefusal)
        {
            return proofRefusal;
        }

        var authentication = ceremony.Authentication!;
        var reserved = await lookups.Find(digester.Digest(authentication.IdpIssuer, authentication.Subject)) is not null;
        await decisions.Get<AccountGuard>(new ReadModelKey(existing.Account.Value));
        var state = await accounts.FindLinkState(existing.Account) ?? new(existing.Account.Value);
        return new Decision(existing.Account, state, authentication, ceremony.Intent, reserved, options.Value.Accounts.LinkHoldMinimum, time.GetUtcNow());
    }

    /// <summary>
    /// Produces proposal and optional quarantine events without appending them.
    /// </summary>
    /// <param name="decision">The guarded state and trusted ceremony observations.</param>
    /// <returns>The events and proposal identifier, or a refusal without events.</returns>
    public Result<(IEnumerable<EventForEventSourceId>, LinkProposalId), ValidationResult> Handle(Decision decision)
    {
        if (LinkDecisions.RequireActive(decision.State) is { } refusal)
        {
            return refusal;
        }

        var authentication = decision.Authentication;
        if (decision.State.Proposals.Values.Any(proposal => proposal.Matches(authentication) && proposal.State != "rejected"))
        {
            return ValidationResult.Error("proposal_already_exists");
        }

        LinkProposalId id = Guid.NewGuid();
        var events = new List<object> { new UpstreamKeyLinkProposed(id, authentication.ConnectionId, authentication.IdpIssuer, authentication.Subject) };
        if (decision.Reserved || decision.Intent.SoftDisabledConnection)
        {
            events.Add(new UpstreamKeyLinkQuarantined(id, decision.Reserved ? "upstream_key_already_linked" : "soft_disabled_connection", Guid.NewGuid(), decision.At + decision.Hold));
        }

        return (events.Select(@event => new EventForEventSourceId(decision.Account.Value, @event) { Occurred = decision.At }).ToArray(), id);
    }

    /// <summary>
    /// Guarded account state and trusted observations for the proposal decision.
    /// </summary>
    /// <param name="Account">The authenticated account subject.</param>
    /// <param name="State">The guarded account link state.</param>
    /// <param name="Authentication">The fresh session-bound upstream proof.</param>
    /// <param name="Intent">The explicit ceremony intent and trusted connection policy.</param>
    /// <param name="Reserved">Whether the upstream key already has a permanent reservation.</param>
    /// <param name="Hold">The realm's minimum link hold duration.</param>
    /// <param name="At">The trusted decision time.</param>
    internal record Decision(AccountSubject Account, AccountLinkState State, VerifiedAuthentication Authentication, LinkIntent Intent, bool Reserved, TimeSpan Hold, DateTimeOffset At);
}
