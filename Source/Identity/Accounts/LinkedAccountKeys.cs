// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Identity.Accounts;

/// <summary>The current user's linked-key display, never an account-matching or authorization surface.</summary>
/// <param name="LinkId">The confirmed link.</param>
/// <param name="ConnectionId">The connection reference.</param>
/// <param name="IdpIssuer">The authority.</param>
/// <param name="DisplayIdentifier">The protected display identifier.</param>
/// <param name="LinkedAt">The confirmation time, absent before confirmation.</param>
/// <param name="State">The proposal or confirmed-link state.</param>
[ReadModel]
public record LinkedAccountKeys(LinkId LinkId, ConnectionId ConnectionId, UpstreamIssuer IdpIssuer, Login? DisplayIdentifier, DateTimeOffset? LinkedAt, string State = "confirmed")
{
    /// <summary>Gets only active keys belonging to the authenticated account, never a supplied target subject.</summary>
    /// <param name="context">The authenticated request.</param>
    /// <param name="readModels">The separate pending-proposal read model.</param>
    /// <param name="accounts">The canonical account reader.</param>
    /// <returns>The current account's linked keys.</returns>
    /// <exception cref="AccountOperationRefused">The authenticated account is unavailable.</exception>
    [Authorize]
    public static async Task<IEnumerable<LinkedAccountKeys>> ForCurrentAccount(IHttpContextAccessor context, IReadModels readModels, AccountReader accounts)
    {
        var principal = context.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new AccountOperationRefused("authenticated_account_required");
        }

        if (!AccountSubject.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var account))
        {
            return [];
        }

        var owner = await accounts.FindActive(account!) ?? throw new AccountOperationRefused("account_not_active");

        var links = owner.LinkedUpstreamKeys.Select(link => new LinkedAccountKeys(link.LinkId, link.ConnectionId, link.IdpIssuer, link.Login, link.LinkedAt));

        // The S2 linking fold remains an explicit pending-proposal source only. Confirmed key
        // identity, profile and lifecycle all come from the canonical Account projection.
        var pending = await readModels.GetInstanceById<AccountLinkState>(new ReadModelKey(account!.Value));
        var proposals = (pending?.Proposals.Values ?? []).Where(proposal => !proposal.Terminal)
            .Select(proposal => new LinkedAccountKeys(new(proposal.Id.Value), proposal.Connection, proposal.Issuer, null, null, proposal.State));
        return links.Concat(proposals).ToArray();
    }
}
