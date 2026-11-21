// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;
using Cratis.Monads;

namespace Cratis.Identity.Accounts.Linking.Confirming;

/// <summary>Shared confirmation policy and atomic event construction; never appends.</summary>
internal static class LinkConfirmation
{
    /// <summary>Requires an active owner and the exact proposed upstream tuple.</summary>
    /// <param name="decision">The guarded inputs.</param>
    /// <param name="id">The owner's proposal identifier.</param>
    /// <returns>The matching proposal or its closed refusal.</returns>
    internal static Result<LinkProposal, ValidationResult> RequireProposal(Decision decision, LinkProposalId id)
    {
        if (AccountLinkState.RequireActive(decision.State) is { } refusal)
        {
            return refusal;
        }

        if (!decision.State.Proposals.TryGetValue(id.Value, out var proposal))
        {
            return ValidationResult.Error("proposal_not_found");
        }

        if (!proposal.Matches(decision.Authentication))
        {
            return ValidationResult.Error("upstream_proof_mismatch");
        }

        return proposal;
    }

    /// <summary>Requires the current realm hold and post-hold proof boundary.</summary>
    /// <param name="decision">The guarded inputs.</param>
    /// <param name="proposal">The matching proposal.</param>
    /// <returns>A refusal, or null when the boundary is satisfied.</returns>
    internal static ValidationResult? RequireHold(Decision decision, LinkProposal proposal)
    {
        return LinkProposal.RequireHold(proposal, decision.Authentication, decision.Hold, decision.At);
    }

    /// <summary>Builds an atomic clearance/link batch or a reservation quarantine batch.</summary>
    /// <param name="decision">The guarded inputs.</param>
    /// <param name="proposal">The eligible proposal.</param>
    /// <param name="clearance">The optional resolver clearance fact.</param>
    /// <returns>The events and post-commit outcome.</returns>
    internal static (IEnumerable<EventForEventSourceId>, LinkConfirmationOutcome) Commit(Decision decision, LinkProposal proposal, IEnumerable<object> clearance)
    {
        if (decision.Reserved)
        {
            return (ForAccount(decision, [new UpstreamKeyLinkQuarantined(proposal.Id, "upstream_key_already_linked", Guid.NewGuid(), decision.At + decision.Hold)]), new(null));
        }

        LinkId link = proposal.Id.Value;
        var authentication = decision.Authentication;
        var events = clearance.Concat([
            new UpstreamKeyLinkRecorded(link, proposal.Connection, proposal.Issuer, decision.Digest, authentication.OperationId.Value, authentication.AuthTime, FactId.ForOperation(new(proposal.Id.Value), nameof(UpstreamKeyLinkRecorded))),
            new UpstreamKeySubjectRecorded(link, proposal.Issuer, proposal.Subject)]);
        return (ForAccount(decision, events), new(link));
    }

    static EventForEventSourceId[] ForAccount(Decision decision, IEnumerable<object> events) =>
        events.Select(@event => new EventForEventSourceId(decision.Account.Value, @event) { Occurred = decision.At }).ToArray();

    /// <summary>The guarded inputs for committing a confirmed or cleared proposal.</summary>
    /// <param name="Account">The authenticated owner.</param>
    /// <param name="State">The state read after enrolling the account guard.</param>
    /// <param name="Authentication">The fresh owner-session-bound upstream proof.</param>
    /// <param name="Digest">The exact upstream key digest.</param>
    /// <param name="Reserved">Whether the permanent reservation is already held.</param>
    /// <param name="Hold">The realm's minimum hold.</param>
    /// <param name="At">The trusted decision time.</param>
    internal record Decision(AccountSubject Account, AccountLinkState State, VerifiedAuthentication Authentication, UpstreamLookupDigest Digest, bool Reserved, TimeSpan Hold, DateTimeOffset At);
}
