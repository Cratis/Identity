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
[Command, ProtocolCommand, ExcludeFromDiscovery, ProtectedDecision]
internal record UnlinkUpstreamKey(LinkId LinkId)
{
    public async Task<Result<UnlinkDecision, ValidationResult>> Provide(ILinkCeremony ceremony, IDecisionReads decisions, IReadModels readModels, TimeProvider time)
    {
        var session = LinkDecisions.RequireSession(ceremony.Session, time);
        if (session.TryGetError(out var refusal))
        {
            return refusal;
        }

        var account = session.AsT0.Account;
        await decisions.Get<AccountGuard>(new ReadModelKey(account.Value));
        var state = await readModels.GetInstanceById<AccountLinkState>(new ReadModelKey(account.Value)) ?? new(account.Value);
        return new UnlinkDecision(account, state, time.GetUtcNow());
    }

    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(UnlinkDecision decision)
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
}

/// <summary>
/// Guarded confirmed links and the trusted unlink time.
/// </summary>
internal record UnlinkDecision(AccountSubject Account, AccountLinkState State, DateTimeOffset At);
