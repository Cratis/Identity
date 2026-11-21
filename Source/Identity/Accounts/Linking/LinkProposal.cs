// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>The exact key and proof boundary for one explicit proposal.</summary>
/// <param name="Id">The proposal.</param>
/// <param name="Connection">The connection.</param>
/// <param name="Issuer">The authority.</param>
/// <param name="Subject">The protected subject.</param>
/// <param name="ProposedAt">The server-observed proposal instant.</param>
/// <param name="HoldNotBefore">The hold boundary.</param>
/// <param name="State">The lifecycle state.</param>
internal record LinkProposal(LinkProposalId Id, ConnectionId Connection, UpstreamIssuer Issuer, UpstreamSubject Subject, DateTimeOffset ProposedAt, DateTimeOffset HoldNotBefore, string State)
{
    /// <summary>Gets whether the proposal can never progress.</summary>
    internal bool Terminal => State == "confirmed" || State == "rejected";

    /// <summary>
    /// Requires upstream proof matching the proposed key.
    /// </summary>
    /// <param name="proposal">The account's proposal.</param>
    /// <param name="authentication">The adapter-verified upstream proof.</param>
    /// <returns>A refusal, or null when the proof matches.</returns>
    internal static ValidationResult? RequireMatchingProof(LinkProposal proposal, VerifiedAuthentication authentication) => proposal.Matches(authentication)
        ? null
        : ValidationResult.Error("upstream_proof_mismatch");

    /// <summary>
    /// Requires the configured hold to have elapsed and proof issued after its boundary.
    /// </summary>
    /// <param name="proposal">The account's proposal.</param>
    /// <param name="authentication">The adapter-verified upstream proof.</param>
    /// <param name="minimum">The realm's minimum hold duration.</param>
    /// <param name="now">The trusted current time.</param>
    /// <returns>A refusal, or null when the hold and post-hold proof are satisfied.</returns>
    internal static ValidationResult? RequireHold(LinkProposal proposal, VerifiedAuthentication authentication, TimeSpan minimum, DateTimeOffset now)
    {
        var hold = proposal.ProposedAt + minimum;
        var boundary = hold > proposal.HoldNotBefore ? hold : proposal.HoldNotBefore;
        if (now < boundary)
        {
            return ValidationResult.Error("realm_configured_hold_not_elapsed");
        }

        return authentication.AuthTime >= boundary ? null : ValidationResult.Error("fresh_post_hold_proof_required");
    }

    /// <summary>Checks the complete immutable upstream tuple.</summary>
    /// <param name="authentication">The adapter proof.</param>
    /// <returns>Whether the proof belongs to this proposal.</returns>
    internal bool Matches(VerifiedAuthentication authentication) => Connection == authentication.ConnectionId && Issuer == authentication.IdpIssuer && Subject == authentication.Subject;

    /// <summary>Checks hold and lifecycle boundaries for confirmation.</summary>
    /// <param name="now">The server clock.</param>
    /// <returns>Whether confirmation is permitted.</returns>
    internal bool Confirmable(DateTimeOffset now) => (State == "pending_proof" || State == "cleared") && now >= HoldNotBefore;

    /// <summary>Checks resolver and post-hold proof timing.</summary>
    /// <param name="now">The server clock.</param>
    /// <param name="authentication">The new-key proof.</param>
    /// <returns>Whether resolver clearance is permitted.</returns>
    internal bool Clearable(DateTimeOffset now, VerifiedAuthentication authentication) => State == "quarantined" && now >= HoldNotBefore && authentication.AuthTime >= HoldNotBefore;
}
