// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Linking.Confirming;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Protocol;
using Cratis.Monads;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.Linking.Clearing;

/// <summary>Clears quarantine only with resolver permission and the target owner's fresh dual proof.</summary>
/// <param name="Account">The target account.</param>
/// <param name="ProposalId">The quarantined proposal.</param>
[Command, ProtocolCommand, ExcludeFromDiscovery, ProtectedDecision]
internal record ClearLinkQuarantine(AccountSubject Account, LinkProposalId ProposalId)
{
    /// <summary>Verifies resolver authority and owner binding before enrolling the decision guard.</summary>
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

        if (!ceremony.Session.HasClaim("permission", "identity:resolve-links"))
        {
            return ValidationResult.Error("authorized_resolver_required");
        }

        var existing = session.AsT0;
        if (existing.Account != Account)
        {
            return ValidationResult.Error("target_account_proof_required");
        }

        if (LinkDecisions.RequireProof(ceremony.Authentication, existing.SessionId, time.GetUtcNow()) is { } proofRefusal)
        {
            return proofRefusal;
        }

        await decisions.Get<AccountGuard>(new ReadModelKey(Account.Value));
        var state = await accounts.FindLinkState(Account) ?? new(Account.Value);
        var authentication = ceremony.Authentication!;
        var digest = digester.Digest(authentication.IdpIssuer, authentication.Subject);
        var reserved = await lookups.Find(digest) is not null;
        return new LinkConfirmationDecision(Account, state, authentication, digest, reserved, options.Value.Accounts.LinkHoldMinimum, time.GetUtcNow());
    }

    /// <summary>Produces clearance and link facts in the same guarded batch, never by elapsed time alone.</summary>
    /// <param name="decision">The guarded inputs.</param>
    /// <returns>The atomic events and post-commit outcome, or a refusal.</returns>
    public Result<(IEnumerable<EventForEventSourceId>, LinkConfirmationOutcome), ValidationResult> Handle(LinkConfirmationDecision decision)
    {
        var found = LinkConfirmation.RequireProposal(decision, ProposalId);
        if (found.TryGetError(out var refusal))
        {
            return refusal;
        }

        var proposal = found.AsT0;
        if (!proposal.Clearable(decision.At, decision.Authentication))
        {
            return ValidationResult.Error("quarantine_not_clearable");
        }

        if (LinkConfirmation.RequireHold(decision, proposal) is { } holdRefusal)
        {
            return holdRefusal;
        }

        return LinkConfirmation.Commit(decision, proposal, [new UpstreamKeyLinkCleared(ProposalId, Guid.NewGuid(), Guid.NewGuid(), "authorized_resolution")]);
    }
}
