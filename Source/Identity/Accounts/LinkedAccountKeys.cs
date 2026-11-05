// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts;

/// <summary>The current user's linked-key display, never an account-matching or authorization surface.</summary>
/// <param name="LinkId">The confirmed link.</param>
/// <param name="ConnectionId">The connection reference.</param>
/// <param name="IdpIssuer">The authority.</param>
/// <param name="DisplayIdentifier">The protected display identifier.</param>
/// <param name="LinkedAt">The confirmation time.</param>
[ReadModel]
public record LinkedAccountKeys(LinkId LinkId, ConnectionId ConnectionId, UpstreamIssuer IdpIssuer, Login? DisplayIdentifier, DateTimeOffset LinkedAt)
{
    /// <summary>Gets only active keys belonging to the authenticated account, never a supplied target subject.</summary>
    /// <param name="context">The authenticated request.</param>
    /// <param name="readModels">The passive account fold.</param>
    /// <param name="options">The explicit account experiment switch.</param>
    /// <param name="environment">The Development boundary.</param>
    /// <returns>The current account's linked keys.</returns>
    /// <exception cref="AccountOperationRefused">The experiment or authenticated account is unavailable.</exception>
    public static async Task<IEnumerable<LinkedAccountKeys>> ForCurrentAccount(IHttpContextAccessor context, IReadModels readModels, IOptions<IdentityOptions> options, IHostEnvironment environment)
    {
        if (!options.Value.Accounts.EventSourced || !environment.IsDevelopment())
        {
            throw new AccountOperationRefused("pending owner decision D18");
        }

        var principal = context.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true || !AccountSubject.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var account))
        {
            throw new AccountOperationRefused("authenticated_account_required");
        }

        var state = await readModels.GetInstanceById<AccountLinkState>(account!.Value);
        UpstreamKeyLinking.RequireActive(state);
        return state.Links.Values.Select(link => new LinkedAccountKeys(link.LinkId, link.ConnectionId, link.IdpIssuer, state.Logins.GetValueOrDefault(link.LinkId.Value), link.LinkedAt)).ToArray();
    }
}
