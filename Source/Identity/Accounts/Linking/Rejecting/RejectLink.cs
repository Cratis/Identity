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

namespace Cratis.Identity.Accounts.Linking.Rejecting;

/// <summary>
/// Terminally rejects a proposal belonging to the authenticated account.
/// </summary>
[Command, ProtocolCommand, ExcludeFromDiscovery, ProtectedDecision]
internal record RejectLink(LinkProposalId ProposalId)
{
    public async Task<Result<LinkRejectionDecision, ValidationResult>> Provide(ILinkCeremony ceremony, IDecisionReads decisions, IReadModels readModels, TimeProvider time)
    {
        var session = LinkDecisions.RequireSession(ceremony.Session, time);
        if (session.TryGetError(out var refusal))
        {
            return refusal;
        }

        var account = session.AsT0.Account;
        await decisions.Get<AccountGuard>(new ReadModelKey(account.Value));
        var state = await readModels.GetInstanceById<AccountLinkState>(new ReadModelKey(account.Value)) ?? new(account.Value);
        return new LinkRejectionDecision(account, state);
    }

    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(LinkRejectionDecision decision)
    {
        if (LinkDecisions.RequireActive(decision.State) is { } refusal)
        {
            return refusal;
        }

        var found = LinkDecisions.FindProposal(decision.State, ProposalId);
        if (found.TryGetError(out refusal))
        {
            return refusal;
        }

        if (found.AsT0.Terminal)
        {
            return ValidationResult.Error("terminal_proposal");
        }

        return new EventForEventSourceId[] { new(decision.Account.Value, new UpstreamKeyLinkRejected(ProposalId, "owner_rejected")) };
    }
}

/// <summary>
/// Guarded state for an owner rejection.
/// </summary>
internal record LinkRejectionDecision(AccountSubject Account, AccountLinkState State);
