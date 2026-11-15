// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Identity.Authorization.ConnectedApps;

/// <summary>Revokes one authorization owned by the signed-in account through the durable grant fence.</summary>
/// <param name="AuthorizationId">The server-generated authorization identifier.</param>
[Command]
[Authorize]
public record RevokeConnectedApp(string AuthorizationId)
{
    /// <summary>Commits and confirms the grant fence before reporting success.</summary>
    /// <param name="http">The realm-bound request.</param>
    /// <param name="revocation">The ownership-checked revocation service.</param>
    /// <returns>The completed revocation.</returns>
    public async Task Handle(IHttpContextAccessor http, ConnectedAppRevocation revocation)
    {
        var principal = http.HttpContext?.User;
        AccountOperationRefused.Require(principal?.Identity?.IsAuthenticated == true, "authentication_required");
        AccountOperationRefused.Require(AccountSubject.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var account), "connected_app_not_available");
        AccountOperationRefused.Require(!string.IsNullOrWhiteSpace(AuthorizationId), "connected_app_not_available");
        await revocation.Revoke(AuthorizationId, account!.Value, http.HttpContext!.RequestAborted);
    }
}
