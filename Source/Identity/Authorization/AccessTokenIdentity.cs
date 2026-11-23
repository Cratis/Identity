// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
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
    /// The assurance level the contract assigns when no connection mapping vouches for a stronger one.
    /// </summary>
    public const string BasicAssurance = "basic";

    /// <summary>
    /// The shortest lifetime an exchanged access token is issued with.
    /// </summary>
    public static readonly TimeSpan MinimumExchangeLifetime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the claim types written to access tokens, in addition to those OpenIddict writes itself
    /// (<c language="text">iss</c>, <c language="text">aud</c>, <c language="text">exp</c>, <c language="text">iat</c>, <c language="text">jti</c>, <c language="text">scope</c>, <c language="text">client_id</c>).
    /// The advisory <c language="text">cid</c> is derived only once an authorization exists, on code redemption and refresh.
    /// </summary>
    public static IEnumerable<string> AccessTokenClaimTypes { get; } =
    [
        Claims.Subject,
        Claims.Name,
        Claims.PreferredUsername,
        Claims.AuthorizedParty,
        IdentityClaims.Idp,
        IdentityClaims.IdpSub,
        IdentityClaims.GitHubId,
        IdentityClaims.GitHubLogin,
        Claims.AuthenticationTime,
        Claims.AuthenticationContextReference,
        IdentityClaims.TenantId,
        CredentialId.ClaimType
    ];

    /// <summary>
    /// Creates the identity for a grant the user just consented to.
    /// </summary>
    /// <param name="account">The consenting <see cref="Account"/>.</param>
    /// <param name="clientId">The client the grant is for.</param>
    /// <param name="grant">The <see cref="Grant"/>.</param>
    /// <param name="tenant">The <see cref="SelectableTenant"/> the user selected.</param>
    /// <param name="authTime">When the upstream authentication of the signed-in session was verified.</param>
    /// <param name="link">The account link the session authenticated through; a grant without one is refused on refresh.</param>
    /// <returns>The <see cref="ClaimsIdentity"/> to sign in with.</returns>
    public static ClaimsIdentity Create(Account account, string clientId, Grant grant, SelectableTenant tenant, DateTimeOffset authTime, Guid? link = null)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        identity.SetClaim(IdentityClaims.TenantId, tenant.Id);
        identity.SetScopes(grant.Scopes);
        identity.SetResources(grant.Resource.Audience);
        SetAuthTime(identity, authTime.ToUnixTimeSeconds());
        AuthenticatingLink.Stamp(identity, link);
        Apply(identity, account, clientId, grant.Resource.ReceivesUpstreamIdentity);
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
    /// <param name="receivesUpstreamIdentity">Whether every audience of the grant is a registered upstream-identity consumer, per the current catalog.</param>
    /// <returns>The <see cref="ClaimsIdentity"/> to sign in with.</returns>
    public static ClaimsIdentity Renew(ClaimsPrincipal granted, Account account, string clientId, bool receivesUpstreamIdentity)
    {
        var identity = new ClaimsIdentity(granted.Claims, TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        Apply(identity, account, clientId, receivesUpstreamIdentity);
        return identity;
    }

    /// <summary>
    /// Creates the identity for an RFC 8693 token exchange. No grant, refresh token or delegation is involved: the
    /// token is bound to one resource, one tenant and the scopes the caller was allowed, and carries the external
    /// issuer as <c language="text">idp</c>. An authentication time is copied only when the subject token had one.
    /// </summary>
    /// <param name="account">The resolved <see cref="Account"/>.</param>
    /// <param name="clientId">The exchange client.</param>
    /// <param name="resource">The <see cref="ResourceServer"/> the token is for.</param>
    /// <param name="tenantId">The listed tenant the token is bound to.</param>
    /// <param name="scopes">The granted scopes.</param>
    /// <param name="externalIssuer">The configured external issuer that vouched for the subject.</param>
    /// <param name="externalSubject">The subject at the external issuer.</param>
    /// <param name="authTime">The subject token's authentication time in Unix seconds, if it carried one.</param>
    /// <param name="accessTokenLifetime">The lifetime of the access token; floored at <see cref="MinimumExchangeLifetime"/>.</param>
    /// <returns>The <see cref="ClaimsIdentity"/> to sign in with.</returns>
    public static ClaimsIdentity Exchange(
        Account account,
        string clientId,
        ResourceServer resource,
        string tenantId,
        IEnumerable<string> scopes,
        string externalIssuer,
        string externalSubject,
        long? authTime,
        TimeSpan accessTokenLifetime)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        identity.SetScopes(scopes.Where(_ => !string.Equals(_, Scopes.OfflineAccess, StringComparison.Ordinal)));
        identity.SetResources(resource.Audience);
        var gitHub = account.LinkFor(GitHubConnection.Issuer);
        identity
            .SetClaim(Claims.Subject, account.Id.Value)
            .SetClaim(Claims.Name, account.Name.Value)
            .SetClaim(Claims.PreferredUsername, resource.ReceivesUpstreamIdentity && string.Equals(externalIssuer, GitHubConnection.Issuer, StringComparison.Ordinal) ? gitHub?.Login.Value : null)
            .SetClaim(IdentityClaims.Idp, externalIssuer)
            .SetClaim(IdentityClaims.IdpSub, resource.ReceivesUpstreamIdentity ? externalSubject : null)
            .SetClaim(Claims.AuthenticationContextReference, BasicAssurance)
            .SetClaim(Claims.AuthorizedParty, clientId)
            .SetClaim(IdentityClaims.TenantId, tenantId);

        if (authTime is { } seconds)
        {
            SetAuthTime(identity, seconds);
        }

        identity.SetDestinations(claim => AccessTokenClaimTypes.Contains(claim.Type, StringComparer.Ordinal) ? [Destinations.AccessToken] : []);
        new ClaimsPrincipal(identity).SetAccessTokenLifetime(accessTokenLifetime < MinimumExchangeLifetime ? MinimumExchangeLifetime : accessTokenLifetime);
        return identity;
    }

    /// <summary>
    /// Gets the original authentication time a grant carries.
    /// </summary>
    /// <param name="granted">The principal carried by the authorization code or refresh token.</param>
    /// <returns>The authentication time in Unix seconds, or <see langword="null"/> if the grant carries none.</returns>
    public static long? AuthTimeOf(ClaimsPrincipal granted) =>
        long.TryParse(granted.FindFirst(Claims.AuthenticationTime)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ? seconds : null;

    static void SetAuthTime(ClaimsIdentity identity, long seconds)
    {
        identity.SetClaim(Claims.AuthenticationTime, (string?)null);
        identity.AddClaim(new Claim(Claims.AuthenticationTime, seconds.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64));
    }

    static void Apply(ClaimsIdentity identity, Account account, string clientId, bool receivesUpstreamIdentity)
    {
        // Only registered upstream-identity consumers get the upstream subject, GitHub claims and preferred_username; everything else fails closed.
        // Deviation from identity-contract-v1.md:148 (name MUST NOT appear in access tokens): resource proxies use it as the identity name;
        // tracked for the v1.1 amendment.
        var authenticatingLink = ResolveAuthenticatingLink(identity, account);
        var gitHub = receivesUpstreamIdentity ? authenticatingLink : null;
        identity
            .SetClaim(Claims.Subject, account.Id.Value)
            .SetClaim(Claims.Name, account.Name.Value)
            .SetClaim(Claims.PreferredUsername, gitHub?.Login.Value)
            .SetClaim(IdentityClaims.Idp, authenticatingLink?.IdpIssuer.Value)
            .SetClaim(IdentityClaims.IdpSub, gitHub?.IdpSub.Value)
            .SetClaim(IdentityClaims.GitHubId, gitHub?.IdpSub.Value)
            .SetClaim(IdentityClaims.GitHubLogin, gitHub?.Login.Value)
            .SetClaim(Claims.AuthenticationContextReference, BasicAssurance)
            .SetClaim(Claims.AuthorizedParty, clientId)
            .SetClaim(CredentialId.ClaimType, CredentialId.For(identity.GetAuthorizationId()));

        identity.SetDestinations(claim => AccessTokenClaimTypes.Contains(claim.Type, StringComparer.Ordinal) ? [Destinations.AccessToken] : []);
    }

    static LinkedUpstreamKey? ResolveAuthenticatingLink(ClaimsIdentity identity, Account account)
    {
        if (AuthenticatingLink.Of(new ClaimsPrincipal(identity)) is not { } linkId)
        {
            return null;
        }

        var links = account.LinkedUpstreamKeys.Where(_ => _.LinkId.Value == linkId).Take(2).ToArray();
        return links is [var link] &&
            link.ConnectionId == GitHubConnection.FixedConnectionId &&
            link.IdpIssuer.Value == GitHubConnection.Issuer ? link : null;
    }
}
