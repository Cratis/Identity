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

    public static ValidationResult? RequireActive(AccountLinkState? state) => state is { Registered: true, Tombstoned: false, Disabled: false }
        ? null
        : ValidationResult.Error("account_not_active");

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

    public static Result<LinkProposal, ValidationResult> FindProposal(AccountLinkState state, LinkProposalId id) =>
        state.Proposals.TryGetValue(id.Value, out var proposal) ? proposal : ValidationResult.Error("proposal_not_found");

    public static ValidationResult? RequireMatchingProof(LinkProposal proposal, VerifiedAuthentication authentication) => proposal.Matches(authentication)
        ? null
        : ValidationResult.Error("upstream_proof_mismatch");

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
