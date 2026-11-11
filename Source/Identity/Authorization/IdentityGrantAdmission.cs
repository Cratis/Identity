// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Checks immutable identity-grant bindings before the common account and refresh-fence admission.</summary>
internal static class IdentityGrantAdmission
{
    /// <summary>Checks a protected code or refresh principal, never caller-supplied claim data.</summary>
    /// <param name="granted">The validated protected principal.</param>
    /// <param name="request">The validated token request.</param>
    /// <param name="options">The current client ceiling and issuance gate.</param>
    /// <returns>Whether the identity-only binding remains admissible.</returns>
    public static bool CanRedeem(ClaimsPrincipal granted, OpenIddictRequest request, IdentityOptions options)
    {
        if (!options.Oidc.IssuanceEnabled) return false;
        if (!HasIdentityBinding(granted, request)) return false;
        var client = options.OidcClients.SingleOrDefault(client => string.Equals(client.ClientId, request.ClientId, StringComparison.Ordinal));
        if (client is null || !granted.HasPresenter(client.ClientId)) return false;
        if (granted.GetScopes().Except(client.EffectiveScopes, StringComparer.Ordinal).Any()) return false;
        var requested = request.GetScopes();
        if (requested.Length == 0) return true;
        return requested.Contains(Scopes.OpenId, StringComparer.Ordinal) && !requested.Except(granted.GetScopes(), StringComparer.Ordinal).Any();
    }

    static bool HasIdentityBinding(ClaimsPrincipal granted, OpenIddictRequest request)
    {
        if (request.GetResources().Length > 0) return false;
        if (!granted.GetResources().SequenceEqual([OidcIdentity.UserInfoAudience], StringComparer.Ordinal)) return false;
        return granted.HasScope(Scopes.OpenId) && granted.GetClaim(IdentityClaims.TenantId) is null;
    }
}
