// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Identity.Authorization.ConnectedApps;

/// <summary>A display-only view of one active authorization for the signed-in account.</summary>
/// <param name="AuthorizationId">The server-generated authorization identifier.</param>
/// <param name="ClientId">The public client identifier.</param>
/// <param name="DisplayName">The client name, falling back to its identifier.</param>
/// <param name="Scopes">The approved scope names.</param>
/// <param name="GrantedAt">When the authorization was created.</param>
[ReadModel]
internal record ConnectedApp(string AuthorizationId, string ClientId, string DisplayName, IReadOnlyList<string> Scopes, DateTimeOffset GrantedAt)
{
    /// <summary>Lists at most 1000 active grants for the authenticated account, newest first.</summary>
    /// <param name="http">The realm-bound request.</param>
    /// <param name="reader">The operational grant reader.</param>
    /// <returns>Display-only connected apps; never credential authority.</returns>
    [Authorize]
    public static async Task<IEnumerable<ConnectedApp>> ForCurrentAccount(IHttpContextAccessor http, ConnectedAppReader reader)
    {
        var principal = http.HttpContext?.User;
        AccountOperationRefused.Require(principal?.Identity?.IsAuthenticated == true, "authentication_required");
        var subject = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!AccountSubject.TryParse(subject, out var account)) return [];
        return await reader.Read(account!.Value, http.HttpContext!.RequestAborted);
    }
}
