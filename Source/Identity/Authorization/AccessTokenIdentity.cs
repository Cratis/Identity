// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts;
using Cratis.Identity.Tenants;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Builds the identity OpenIddict turns into authorization codes, refresh tokens and access tokens.
/// </summary>
/// <remarks>
/// Only the claims listed in <see cref="AccessTokenClaimTypes"/> reach the access token. Everything else stays
/// in the encrypted authorization code and refresh token, which only Identity can read.
/// </remarks>
internal static class AccessTokenIdentity
{
    /// <summary>
    /// Gets the claim types written to access tokens, in addition to those OpenIddict writes itself
    /// (<c language="text">iss</c>, <c language="text">aud</c>, <c language="text">exp</c>, <c language="text">iat</c>, <c language="text">jti</c>, <c language="text">scope</c>, <c language="text">client_id</c>).
    /// </summary>
    public static IEnumerable<string> AccessTokenClaimTypes { get; } =
    [
        Claims.Subject,
        Claims.Name,
        Claims.PreferredUsername,
        Claims.AuthorizedParty,
        IdentityClaims.GitHubId,
        IdentityClaims.GitHubLogin,
        IdentityClaims.TenantId
    ];

    /// <summary>
    /// Creates the identity for a grant the user just consented to.
    /// </summary>
    /// <param name="account">The consenting <see cref="Account"/>.</param>
    /// <param name="clientId">The client the grant is for.</param>
    /// <param name="grant">The <see cref="Grant"/>.</param>
    /// <param name="tenant">The <see cref="SelectableTenant"/> the user selected.</param>
    /// <returns>The <see cref="ClaimsIdentity"/> to sign in with.</returns>
    public static ClaimsIdentity Create(Account account, string clientId, Grant grant, SelectableTenant tenant)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        identity.SetClaim(IdentityClaims.TenantId, tenant.Id);
        identity.SetScopes(grant.Scopes);
        identity.SetResources(grant.Resource.Audience);
        Apply(identity, account, clientId);
        return identity;
    }

    /// <summary>
    /// Creates the identity for a token request that redeems an authorization code or a refresh token. The
    /// grant (scopes, resource, tenant and the authorization it belongs to) is kept; account claims are
    /// refreshed from the current account.
    /// </summary>
    /// <param name="granted">The principal carried by the authorization code or refresh token.</param>
    /// <param name="account">The current <see cref="Account"/>.</param>
    /// <param name="clientId">The client redeeming the token.</param>
    /// <returns>The <see cref="ClaimsIdentity"/> to sign in with.</returns>
    public static ClaimsIdentity Renew(ClaimsPrincipal granted, Account account, string clientId)
    {
        var identity = new ClaimsIdentity(granted.Claims, TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        Apply(identity, account, clientId);
        return identity;
    }

    static void Apply(ClaimsIdentity identity, Account account, string clientId)
    {
        identity
            .SetClaim(Claims.Subject, account.Id.Value)
            .SetClaim(Claims.Name, account.Name.Value)
            .SetClaim(Claims.PreferredUsername, account.GitHubLogin.Value)
            .SetClaim(IdentityClaims.GitHubId, account.GitHubId.Value)
            .SetClaim(IdentityClaims.GitHubLogin, account.GitHubLogin.Value)
            .SetClaim(Claims.AuthorizedParty, clientId);

        identity.SetDestinations(claim => AccessTokenClaimTypes.Contains(claim.Type, StringComparer.Ordinal) ? [Destinations.AccessToken] : []);
    }
}
