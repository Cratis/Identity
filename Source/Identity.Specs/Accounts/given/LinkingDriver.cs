// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.given;

/// <summary>Drives linking ceremonies through the current implementation without coupling specs to it.</summary>
/// <param name="linking">The current linking implementation.</param>
internal class LinkingDriver(UpstreamKeyLinking linking)
{
    /// <summary>Proposes a link with explicit session-bound proof.</summary>
    /// <param name="session">The owning account session.</param>
    /// <param name="authentication">The verified upstream proof.</param>
    /// <param name="intent">The ceremony intent and connection policy.</param>
    /// <returns>The proposal reference.</returns>
    public Task<LinkProposalId> Propose(ClaimsPrincipal session, VerifiedAuthentication authentication, LinkIntent intent) => linking.Propose(session, authentication, intent);

    /// <summary>Confirms a proposal with fresh proof.</summary>
    /// <param name="session">The owning account session.</param>
    /// <param name="proposalId">The proposal reference.</param>
    /// <param name="authentication">The verified upstream proof.</param>
    /// <returns>The confirmed link reference.</returns>
    public Task<LinkId> Confirm(ClaimsPrincipal session, LinkProposalId proposalId, VerifiedAuthentication authentication) => linking.Confirm(session, proposalId, authentication);

    /// <summary>Clears a quarantined proposal as an authorized resolver.</summary>
    /// <param name="resolver">The target owner's resolver session.</param>
    /// <param name="account">The target account.</param>
    /// <param name="proposalId">The proposal reference.</param>
    /// <param name="authentication">The verified upstream proof.</param>
    /// <returns>The completion.</returns>
    public Task Clear(ClaimsPrincipal resolver, AccountSubject account, LinkProposalId proposalId, VerifiedAuthentication authentication) => linking.Clear(resolver, account, proposalId, authentication);

    /// <summary>Rejects a proposal without granting a link.</summary>
    /// <param name="session">The owning account session.</param>
    /// <param name="proposalId">The proposal reference.</param>
    /// <returns>The completion.</returns>
    public Task Reject(ClaimsPrincipal session, LinkProposalId proposalId) => linking.Reject(session, proposalId);

    /// <summary>Unlinks a key without releasing its reservation.</summary>
    /// <param name="session">The owning account session.</param>
    /// <param name="linkId">The confirmed link reference.</param>
    /// <returns>The completion.</returns>
    public Task Unlink(ClaimsPrincipal session, LinkId linkId) => linking.Unlink(session, linkId);
}
