// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;
using Cratis.Monads;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>
/// Pure link-policy rules shared by the native commands.
/// </summary>
internal static class LinkDecisions
{
    /// <summary>
    /// Requires a current authenticated account session.
    /// </summary>
    /// <param name="principal">The existing account principal.</param>
    /// <param name="time">The trusted clock.</param>
    /// <returns>The verified session, or its refusal.</returns>
    public static Result<AccountSession, ValidationResult> RequireSession(ClaimsPrincipal principal, TimeProvider time)
    {
        try
        {
            return AccountSession.Require(principal, time);
        }
        catch (AccountOperationRefused refusal)
        {
            return ValidationResult.Error(refusal.Message);
        }
    }

    /// <summary>
    /// Refuses missing, disabled, or terminal accounts.
    /// </summary>
    /// <param name="state">The guarded account state.</param>
    /// <returns>A refusal, or null when the account is active.</returns>
    public static ValidationResult? RequireActive(AccountLinkState? state) => state is { Registered: true, Tombstoned: false, Disabled: false }
        ? null
        : ValidationResult.Error("account_not_active");

    /// <summary>
    /// Requires fresh upstream proof bound to the existing session.
    /// </summary>
    /// <param name="authentication">The adapter-verified upstream proof.</param>
    /// <param name="sessionId">The existing session identifier.</param>
    /// <param name="now">The trusted current time.</param>
    /// <returns>A refusal, or null when the proof is fresh and session-bound.</returns>
    public static ValidationResult? RequireProof(VerifiedAuthentication? authentication, string sessionId, DateTimeOffset now)
    {
        if (authentication is null || authentication.SessionId != sessionId)
        {
            return ValidationResult.Error("fresh_session_bound_upstream_proof_required");
        }

        var age = now - authentication.AuthTime;
        return age >= TimeSpan.Zero && age <= TimeSpan.FromMinutes(5)
            ? null
            : ValidationResult.Error("fresh_session_bound_upstream_proof_required");
    }

    /// <summary>
    /// Finds a proposal belonging to the guarded account.
    /// </summary>
    /// <param name="state">The guarded account state.</param>
    /// <param name="id">The proposal identifier.</param>
    /// <returns>The proposal, or a not-found refusal.</returns>
    public static Result<LinkProposal, ValidationResult> FindProposal(AccountLinkState state, LinkProposalId id) =>
        state.Proposals.TryGetValue(id.Value, out var proposal) ? proposal : ValidationResult.Error("proposal_not_found");

    /// <summary>
    /// Requires upstream proof matching the proposed key.
    /// </summary>
    /// <param name="proposal">The account's proposal.</param>
    /// <param name="authentication">The adapter-verified upstream proof.</param>
    /// <returns>A refusal, or null when the proof matches.</returns>
    public static ValidationResult? RequireMatchingProof(LinkProposal proposal, VerifiedAuthentication authentication) => proposal.Matches(authentication)
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
    public static ValidationResult? RequireHold(LinkProposal proposal, VerifiedAuthentication authentication, TimeSpan minimum, DateTimeOffset now)
    {
        var hold = proposal.ProposedAt + minimum;
        var boundary = hold > proposal.HoldNotBefore ? hold : proposal.HoldNotBefore;
        if (now < boundary)
        {
            return ValidationResult.Error("realm_configured_hold_not_elapsed");
        }

        return authentication.AuthTime >= boundary ? null : ValidationResult.Error("fresh_post_hold_proof_required");
    }
}
