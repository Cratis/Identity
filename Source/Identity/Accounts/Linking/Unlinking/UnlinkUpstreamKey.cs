// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Protocol;
using Cratis.Monads;

namespace Cratis.Identity.Accounts.Linking.Unlinking;

/// <summary>
/// Removes a key only while another confirmed key remains, preserving its reservation.
/// </summary>
/// <param name="LinkId">The owning account's confirmed link identifier.</param>
[Command, ProtocolCommand, ExcludeFromDiscovery, ProtectedDecision]
internal record UnlinkUpstreamKey(LinkId LinkId)
{
    /// <summary>
    /// Verifies the owner session and enrolls the account guard before reading link state.
    /// </summary>
    /// <param name="ceremony">The trusted scope-local ceremony observations.</param>
    /// <param name="decisions">The command-owned optimistic concurrency guard.</param>
    /// <param name="accounts">The passive account link-state reader.</param>
    /// <param name="time">The trusted clock.</param>
    /// <returns>The guarded owner decision and current time, or a refusal.</returns>
    public async Task<Result<Decision, ValidationResult>> Provide(ILinkCeremony ceremony, IDecisionReads decisions, AccountReader accounts, TimeProvider time)
    {
        var session = LinkDecisions.RequireSession(ceremony.Session, time);
        if (session.TryGetError(out var refusal))
        {
            return refusal;
        }

        var account = session.AsT0.Account;
        await decisions.Get<AccountGuard>(new ReadModelKey(account.Value));
        var state = await accounts.FindLinkState(account) ?? new(account.Value);
        return new Decision(account, state, time.GetUtcNow());
    }

    /// <summary>
    /// Produces an unlink event only when another confirmed key remains.
    /// </summary>
    /// <param name="decision">The guarded owning account state and trusted unlink time.</param>
    /// <returns>The unlink event, or a refusal without events.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(Decision decision)
    {
        if (LinkDecisions.RequireActive(decision.State) is { } refusal)
        {
            return refusal;
        }

        if (decision.State.Links.Count <= 1 || !decision.State.Links.TryGetValue(LinkId.Value, out var link))
        {
            return ValidationResult.Error("last_or_unknown_upstream_key");
        }

        var fact = FactId.ForOperation(new(LinkId.Value), nameof(UpstreamKeyUnlinkRecorded));
        return new EventForEventSourceId[] { new(decision.Account.Value, new UpstreamKeyUnlinkRecorded(LinkId, link.KeyHash, "owner_unlinked", decision.At, fact)) { Occurred = decision.At } };
    }

    /// <summary>
    /// Guarded confirmed links and the trusted unlink time.
    /// </summary>
    /// <param name="Account">The authenticated owning account subject.</param>
    /// <param name="State">The guarded account link state.</param>
    /// <param name="At">The trusted unlink time.</param>
    internal record Decision(AccountSubject Account, AccountLinkState State, DateTimeOffset At);
}
