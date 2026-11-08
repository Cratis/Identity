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
internal class UpstreamKeyLinking(AccountStore store, UpstreamLookups lookups, UpstreamKeyDigester digester, IOptions<IdentityOptions> options, TimeProvider time)
{
    /// <summary>Records explicit intent only after both session-bound proofs are present.</summary>
    /// <param name="session">The existing authenticated account session.</param>
    /// <param name="authentication">The adapter-verified new key.</param>
    /// <param name="intent">The explicit ceremony and trusted connection policy.</param>
    /// <returns>The proposal reference.</returns>
    /// <exception cref="AccountOperationRefused">Intent, proof, account or reservation policy refuses the proposal.</exception>
    public async Task<LinkProposalId> Propose(ClaimsPrincipal session, VerifiedAuthentication authentication, LinkIntent intent)
    {
        AccountOperationRefused.Require(!intent.EmailOnlyProof, "email_is_not_identity_proof");
        var existing = AccountSession.Require(session, time);
        AccountOperationRefused.Require(intent.Explicit, "explicit_dual_proof_required");
        RequireProof(authentication, existing.SessionId);
        var reserved = await IsReserved(authentication);
        var read = await store.Read(existing.Account);
        RequireActive(read.State);
        AccountOperationRefused.Require(!read.State.Proposals.Values.Any(proposal => proposal.Matches(authentication) && proposal.State != "rejected"), "proposal_already_exists");
        LinkProposalId id = Guid.NewGuid();
        var events = new List<object> { new UpstreamKeyLinkProposed(id, authentication.ConnectionId, authentication.IdpIssuer, authentication.Subject) };
        var hold = options.Value.Accounts.LinkHoldMinimum;
        if (reserved || intent.SoftDisabledConnection)
        {
            events.Add(new UpstreamKeyLinkQuarantined(id, reserved ? "upstream_key_already_linked" : "soft_disabled_connection", Guid.NewGuid(), time.GetUtcNow() + hold));
        }

        await store.Append(existing.Account, read, events);
        return id;
    }

    /// <summary>Confirms a cleared or ordinary proposal with current dual proof and a guarded append.</summary>
    /// <param name="session">The existing account session.</param>
    /// <param name="proposalId">The proposal.</param>
    /// <param name="authentication">The fresh new-key proof.</param>
    /// <returns>The confirmed link.</returns>
    /// <exception cref="AccountOperationRefused">The proposal, proof, hold or key reservation refuses confirmation.</exception>
    public async Task<LinkId> Confirm(ClaimsPrincipal session, LinkProposalId proposalId, VerifiedAuthentication authentication)
    {
        var existing = AccountSession.Require(session, time);
        RequireProof(authentication, existing.SessionId);
        var read = await store.Read(existing.Account);
        RequireActive(read.State);
        var proposal = FindProposal(read.State, proposalId);
        RequireMatchingProof(proposal, authentication);
        AccountOperationRefused.Require(proposal.Confirmable(time.GetUtcNow()), "proposal_not_confirmable");
        RequireHold(proposal, authentication);
        return await CommitLink(read, proposal, authentication, []);
    }

    /// <summary>Clears quarantine only through a privileged resolver and fresh proof, never by elapsed time alone.</summary>
    /// <param name="resolver">The authenticated target owner with resolver permission.</param>
    /// <param name="account">The target account.</param>
    /// <param name="proposalId">The proposal.</param>
    /// <param name="authentication">Fresh proof of the proposed key.</param>
    /// <returns>The completion.</returns>
    /// <exception cref="AccountOperationRefused">Resolver authorization, proof or hold policy refuses clearance.</exception>
    public async Task Clear(ClaimsPrincipal resolver, AccountSubject account, LinkProposalId proposalId, VerifiedAuthentication authentication)
    {
        var owner = AccountSession.Require(resolver, time);
        AccountOperationRefused.Require(resolver.HasClaim("permission", "identity:resolve-links"), "authorized_resolver_required");
        AccountOperationRefused.Require(owner.Account == account, "target_account_proof_required");
        RequireProof(authentication, owner.SessionId);
        var read = await store.Read(account);
        RequireActive(read.State);
        var proposal = FindProposal(read.State, proposalId);
        RequireMatchingProof(proposal, authentication);
        AccountOperationRefused.Require(proposal.Clearable(time.GetUtcNow(), authentication), "quarantine_not_clearable");
        RequireHold(proposal, authentication);
        _ = await CommitLink(read, proposal, authentication, [new UpstreamKeyLinkCleared(proposalId, Guid.NewGuid(), Guid.NewGuid(), "authorized_resolution")]);
    }

    /// <summary>Rejects a proposal terminally without linking anything.</summary>
    /// <param name="session">The owning session.</param>
    /// <param name="proposalId">The proposal.</param>
    /// <returns>The completion.</returns>
    /// <exception cref="AccountOperationRefused">The session, account or proposal is not eligible.</exception>
    public async Task Reject(ClaimsPrincipal session, LinkProposalId proposalId)
    {
        var existing = AccountSession.Require(session, time);
        var read = await store.Read(existing.Account);
        RequireActive(read.State);
        AccountOperationRefused.Require(!FindProposal(read.State, proposalId).Terminal, "terminal_proposal");
        await store.Append(existing.Account, read, [new UpstreamKeyLinkRejected(proposalId, "owner_rejected")]);
    }

    /// <summary>Removes a key only while another confirmed key remains. Its reservation is never released.</summary>
    /// <param name="session">The owning session.</param>
    /// <param name="linkId">The confirmed link.</param>
    /// <returns>The completion.</returns>
    /// <exception cref="AccountOperationRefused">This is the last key or the link/account proof is invalid.</exception>
    public async Task Unlink(ClaimsPrincipal session, LinkId linkId)
    {
        var existing = AccountSession.Require(session, time);
        var read = await store.Read(existing.Account);
        RequireActive(read.State);
        AccountOperationRefused.Require(read.State.Links.Count > 1, "last_or_unknown_upstream_key");
        AccountOperationRefused.Require(read.State.Links.ContainsKey(linkId.Value), "last_or_unknown_upstream_key");
        var link = read.State.Links[linkId.Value];
        await store.Append(existing.Account, read, [new UpstreamKeyUnlinkRecorded(linkId, link.KeyHash, "owner_unlinked", time.GetUtcNow(), FactId.ForOperation(new(linkId.Value), nameof(UpstreamKeyUnlinkRecorded)))]);
    }

    /// <summary>Refuses missing, disabled or terminal accounts.</summary>
    /// <param name="state">The guarded state.</param>
    internal static void RequireActive(AccountLinkState? state) => AccountOperationRefused.Require(IsActive(state), "account_not_active");

    static bool IsActive(AccountLinkState? state) => state is { Registered: true, Tombstoned: false, Disabled: false };

    static LinkProposal FindProposal(AccountLinkState state, LinkProposalId id) => state.Proposals.TryGetValue(id.Value, out var proposal) ? proposal : throw new AccountOperationRefused("proposal_not_found");

    static void RequireMatchingProof(LinkProposal proposal, VerifiedAuthentication authentication) => AccountOperationRefused.Require(proposal.Matches(authentication), "upstream_proof_mismatch");

    async Task<bool> IsReserved(VerifiedAuthentication authentication) => await lookups.Find(digester.Digest(authentication.IdpIssuer, authentication.Subject)) is not null;

    void RequireHold(LinkProposal proposal, VerifiedAuthentication authentication)
    {
        var hold = proposal.ProposedAt + options.Value.Accounts.LinkHoldMinimum;
        var boundary = hold > proposal.HoldNotBefore ? hold : proposal.HoldNotBefore;
        AccountOperationRefused.Require(time.GetUtcNow() >= boundary, "realm_configured_hold_not_elapsed");
        AccountOperationRefused.Require(authentication.AuthTime >= boundary, "fresh_post_hold_proof_required");
    }

    async Task<LinkId> CommitLink(AccountStore.GuardedAccountRead read, LinkProposal proposal, VerifiedAuthentication authentication, IEnumerable<object> clearance)
    {
        AccountSubject account = new(read.State.Id);
        LinkId link = proposal.Id.Value;
        var events = clearance.Concat([
            new UpstreamKeyLinkRecorded(link, proposal.Connection, proposal.Issuer, digester.Digest(proposal.Issuer, proposal.Subject), authentication.OperationId.Value, authentication.AuthTime, FactId.ForOperation(new(proposal.Id.Value), nameof(UpstreamKeyLinkRecorded))),
            new UpstreamKeySubjectRecorded(link, proposal.Issuer, proposal.Subject)]);
        if (!await IsReserved(authentication) && await store.TryAppend(account, read, events))
        {
            return link;
        }

        var current = await store.Read(account);
        RequireActive(current.State);
        await store.Append(account, current, [new UpstreamKeyLinkQuarantined(proposal.Id, "upstream_key_already_linked", Guid.NewGuid(), time.GetUtcNow() + options.Value.Accounts.LinkHoldMinimum)]);
        throw new AccountOperationRefused("upstream_key_already_reserved");
    }

    void RequireProof(VerifiedAuthentication authentication, string sessionId)
    {
        if (authentication is null || authentication.SessionId != sessionId)
        {
            throw new AccountOperationRefused("fresh_session_bound_upstream_proof_required");
        }

        var age = time.GetUtcNow() - authentication.AuthTime;
        AccountOperationRefused.Require(age >= TimeSpan.Zero && age <= TimeSpan.FromMinutes(5), "fresh_session_bound_upstream_proof_required");
    }
}
