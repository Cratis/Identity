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
/// <param name="ProposalId">The owning account's proposal identifier.</param>
[Command, ProtocolCommand, ExcludeFromDiscovery, ProtectedDecision]
internal record RejectLink(LinkProposalId ProposalId)
{
    /// <summary>
    /// Verifies the owner session and enrolls the account guard before reading link state.
    /// </summary>
    /// <param name="ceremony">The trusted scope-local ceremony observations.</param>
    /// <param name="decisions">The command-owned optimistic concurrency guard.</param>
    /// <param name="accounts">The passive account link-state reader.</param>
    /// <param name="time">The trusted clock.</param>
    /// <returns>The guarded owner decision, or a refusal.</returns>
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
        return new Decision(account, state);
    }

    /// <summary>
    /// Produces a terminal rejection event without appending it.
    /// </summary>
    /// <param name="decision">The guarded owning account state.</param>
    /// <returns>The rejection event, or a refusal without events.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(Decision decision)
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

    /// <summary>
    /// Guarded state for an owner rejection.
    /// </summary>
    /// <param name="Account">The authenticated owning account subject.</param>
    /// <param name="State">The guarded account link state.</param>
    internal record Decision(AccountSubject Account, AccountLinkState State);
}
