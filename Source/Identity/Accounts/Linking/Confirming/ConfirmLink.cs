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
using Cratis.Identity.Protocol;
using Cratis.Monads;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.Linking.Confirming;

/// <summary>Confirms a pending proposal using fresh dual proof after the realm hold.</summary>
/// <param name="ProposalId">The owning account's proposal.</param>
[Command, ProtocolCommand, ExcludeFromDiscovery, ProtectedDecision]
internal record ConfirmLink(LinkProposalId ProposalId)
{
    /// <summary>Verifies proof and enrolls the account guard before reading decision state.</summary>
    /// <param name="ceremony">The trusted scope-local ceremony.</param>
    /// <param name="decisions">The command-owned decision reads.</param>
    /// <param name="accounts">The passive account reader.</param>
    /// <param name="lookups">The permanent reservation hints.</param>
    /// <param name="digester">The exact upstream key digester.</param>
    /// <param name="options">The realm hold policy.</param>
    /// <param name="time">The trusted clock.</param>
    /// <returns>The guarded decision or a refusal.</returns>
    public async Task<Result<LinkConfirmationDecision, ValidationResult>> Provide(ILinkCeremony ceremony, IDecisionReads decisions, AccountReader accounts, UpstreamLookups lookups, UpstreamKeyDigester digester, IOptions<IdentityOptions> options, TimeProvider time)
    {
        var session = LinkDecisions.RequireSession(ceremony.Session, time);
        if (session.TryGetError(out var refusal))
        {
            return refusal;
        }

        var existing = session.AsT0;
        if (LinkDecisions.RequireProof(ceremony.Authentication, existing.SessionId, time.GetUtcNow()) is { } proofRefusal)
        {
            return proofRefusal;
        }

        await decisions.Get<AccountGuard>(new ReadModelKey(existing.Account.Value));
        var state = await accounts.FindLinkState(existing.Account) ?? new(existing.Account.Value);
        var authentication = ceremony.Authentication!;
        var digest = digester.Digest(authentication.IdpIssuer, authentication.Subject);
        var reserved = await lookups.Find(digest) is not null;
        return new LinkConfirmationDecision(existing.Account, state, authentication, digest, reserved, options.Value.Accounts.LinkHoldMinimum, time.GetUtcNow());
    }

    /// <summary>Produces a guarded link batch or a quarantine batch for a reserved key.</summary>
    /// <param name="decision">The guarded inputs.</param>
    /// <returns>The events and post-commit outcome, or a refusal.</returns>
    public Result<(IEnumerable<EventForEventSourceId>, LinkConfirmationOutcome), ValidationResult> Handle(LinkConfirmationDecision decision)
    {
        var found = LinkConfirmation.RequireProposal(decision, ProposalId);
        if (found.TryGetError(out var refusal))
        {
            return refusal;
        }

        var proposal = found.AsT0;
        if (!proposal.Confirmable(decision.At))
        {
            return ValidationResult.Error("proposal_not_confirmable");
        }

        if (LinkConfirmation.RequireHold(decision, proposal) is { } holdRefusal)
        {
            return holdRefusal;
        }

        return LinkConfirmation.Commit(decision, proposal, []);
    }
}
