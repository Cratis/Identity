// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>Explicit session-bound dual-proof linking. Never an Arc command or an email matcher.</summary>
/// <param name="store">The guarded store.</param>
/// <param name="lookups">The permanent reservation hints.</param>
/// <param name="digester">The immutable key digester.</param>
/// <param name="options">The realm hold policy.</param>
/// <param name="time">The trusted clock.</param>
public class UpstreamKeyLinking(AccountStore store, UpstreamLookups lookups, UpstreamKeyDigester digester, IOptions<IdentityOptions> options, TimeProvider time)
{
    /// <summary>Records explicit intent only after both session-bound proofs are present.</summary>
    /// <param name="session">The existing authenticated account session.</param>
    /// <param name="authentication">The adapter-verified new key.</param>
    /// <param name="explicitIntent">Whether this is an explicit linking ceremony.</param>
    /// <param name="emailOnlyProof">Whether the caller has only an email hint, which is refused.</param>
    /// <param name="softDisabledConnection">Whether policy requires resolver clearance.</param>
    /// <returns>The proposal reference.</returns>
    public async Task<LinkProposalId> Propose(ClaimsPrincipal session, VerifiedAuthentication authentication, bool explicitIntent, bool emailOnlyProof = false, bool softDisabledConnection = false)
    {
        var existing = AccountSession.Require(session, time);
        if (!explicitIntent || emailOnlyProof)
        {
            throw new AccountOperationRefused("explicit_dual_proof_required");
        }
        RequireProof(authentication, existing.SessionId);
        if (await lookups.Find(digester.Digest(authentication.IdpIssuer, authentication.Subject)) is not null)
        {
            throw new AccountOperationRefused("upstream_key_already_reserved");
        }
        var read = await store.Read(existing.Account);
        RequireActive(read.State);
        if (read.State.Proposals.Values.Any(proposal => proposal.Issuer == authentication.IdpIssuer && proposal.Subject == authentication.Subject && proposal.State != "rejected"))
        {
            throw new AccountOperationRefused("proposal_already_exists");
        }
        LinkProposalId id = Guid.NewGuid();
        var events = new List<object> { new UpstreamKeyLinkProposed(id, authentication.ConnectionId, authentication.IdpIssuer, authentication.Subject) };
        var hold = options.Value.Accounts.LinkHoldMinimum;
        if (hold > TimeSpan.Zero || softDisabledConnection)
        {
            events.Add(new UpstreamKeyLinkQuarantined(id, softDisabledConnection ? "soft_disabled_connection" : "realm_link_hold", Guid.NewGuid(), time.GetUtcNow() + hold));
        }
        await store.Append(existing.Account, read, events);
        return id;
    }

    /// <summary>Confirms a cleared or ordinary proposal with current dual proof and a guarded append.</summary>
    /// <param name="session">The existing account session.</param>
    /// <param name="proposalId">The proposal.</param>
    /// <param name="authentication">The fresh new-key proof.</param>
    /// <returns>The confirmed link.</returns>
    public async Task<LinkId> Confirm(ClaimsPrincipal session, LinkProposalId proposalId, VerifiedAuthentication authentication)
    {
        var existing = AccountSession.Require(session, time);
        RequireProof(authentication, existing.SessionId);
        var read = await store.Read(existing.Account);
        RequireActive(read.State);
        var proposal = FindProposal(read.State, proposalId);
        RequireMatchingProof(proposal, authentication);
        if (proposal.State is not ("pending_proof" or "cleared") || time.GetUtcNow() < proposal.HoldNotBefore)
        {
            throw new AccountOperationRefused("proposal_not_confirmable");
        }
        if (proposal.HoldNotBefore > proposal.ProposedAt && authentication.AuthTime < proposal.HoldNotBefore)
        {
            throw new AccountOperationRefused("fresh_post_hold_proof_required");
        }
        var digest = digester.Digest(proposal.Issuer, proposal.Subject);
        if (await lookups.Find(digest) is not null)
        {
            throw new AccountOperationRefused("upstream_key_already_reserved");
        }
        LinkId link = proposalId.Value;
        await store.Append(existing.Account, read, [
            new UpstreamKeyLinkRecorded(link, proposal.Connection, proposal.Issuer, digest, Guid.NewGuid(), time.GetUtcNow()),
            new UpstreamKeySubjectRecorded(link, proposal.Issuer, proposal.Subject)]);
        return link;
    }

    /// <summary>Clears quarantine only through a privileged resolver and fresh proof, never by elapsed time alone.</summary>
    /// <param name="resolver">The authenticated resolver.</param>
    /// <param name="account">The target account.</param>
    /// <param name="proposalId">The proposal.</param>
    /// <param name="authentication">Fresh proof of the proposed key.</param>
    /// <returns>The completion.</returns>
    public async Task Clear(ClaimsPrincipal resolver, AccountSubject account, LinkProposalId proposalId, VerifiedAuthentication authentication)
    {
        _ = AccountSession.Require(resolver, time);
        if (!resolver.HasClaim("permission", "identity:resolve-links"))
        {
            throw new AccountOperationRefused("authorized_resolver_required");
        }
        RequireProof(authentication);
        var read = await store.Read(account);
        RequireActive(read.State);
        var proposal = FindProposal(read.State, proposalId);
        RequireMatchingProof(proposal, authentication);
        if (proposal.State != "quarantined" || time.GetUtcNow() < proposal.HoldNotBefore || authentication.AuthTime < proposal.HoldNotBefore)
        {
            throw new AccountOperationRefused("quarantine_not_clearable");
        }
        await store.Append(account, read, [new UpstreamKeyLinkCleared(proposalId, Guid.NewGuid(), Guid.NewGuid(), "authorized_resolution")]);
    }

    /// <summary>Rejects a proposal terminally without linking anything.</summary>
    /// <param name="session">The owning session.</param>
    /// <param name="proposalId">The proposal.</param>
    /// <returns>The completion.</returns>
    public async Task Reject(ClaimsPrincipal session, LinkProposalId proposalId)
    {
        var existing = AccountSession.Require(session, time);
        var read = await store.Read(existing.Account);
        RequireActive(read.State);
        var proposal = FindProposal(read.State, proposalId);
        if (proposal.State is "confirmed" or "rejected")
        {
            throw new AccountOperationRefused("terminal_proposal");
        }
        await store.Append(existing.Account, read, [new UpstreamKeyLinkRejected(proposalId, "owner_rejected")]);
    }

    /// <summary>Removes a key only while another confirmed key remains. The reservation is never released.</summary>
    /// <param name="session">The owning session.</param>
    /// <param name="linkId">The confirmed link.</param>
    /// <returns>The completion.</returns>
    public async Task Unlink(ClaimsPrincipal session, LinkId linkId)
    {
        var existing = AccountSession.Require(session, time);
        var read = await store.Read(existing.Account);
        RequireActive(read.State);
        if (read.State.Links.Count <= 1 || !read.State.Links.TryGetValue(linkId.Value, out var link))
        {
            throw new AccountOperationRefused("last_or_unknown_upstream_key");
        }
        await store.Append(existing.Account, read, [new UpstreamKeyUnlinkRecorded(linkId, link.KeyHash, "owner_unlinked", time.GetUtcNow())]);
    }

    internal static void RequireActive(AccountLinkState? state)
    {
        if (state is null || !state.Registered || state.Tombstoned || state.Disabled)
        {
            throw new AccountOperationRefused("account_not_active");
        }
    }

    static LinkProposal FindProposal(AccountLinkState state, LinkProposalId id) => state.Proposals.TryGetValue(id.Value, out var proposal) ? proposal : throw new AccountOperationRefused("proposal_not_found");

    static void RequireMatchingProof(LinkProposal proposal, VerifiedAuthentication authentication)
    {
        if (proposal.Connection != authentication.ConnectionId || proposal.Issuer != authentication.IdpIssuer || proposal.Subject != authentication.Subject)
        {
            throw new AccountOperationRefused("upstream_proof_mismatch");
        }
    }

    void RequireProof(VerifiedAuthentication authentication, string? sessionId = null)
    {
        var age = time.GetUtcNow() - authentication.AuthTime;
        if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(5) || (sessionId is not null && authentication.SessionId != sessionId))
        {
            throw new AccountOperationRefused("fresh_session_bound_upstream_proof_required");
        }
    }
}
