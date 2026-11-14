// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Builds identity-only grants without resource, tenant, or upstream identity claims.</summary>
internal static class OidcIdentity
{
    /// <summary>The fixed audience accepted only by Identity's UserInfo endpoint.</summary>
    public const string UserInfoAudience = "cratis-identity-userinfo";

    /// <summary>The standard OIDC session identifier claim.</summary>
    public const string SessionIdClaim = "sid";

    /// <summary>The private discriminator retained only in protected codes and refresh tokens.</summary>
    public const string GrantMarker = "cratis_identity_only";

    /// <summary>Creates a grant from the current, verified site authentication.</summary>
    /// <param name="authentication">The active account and its verified site authentication.</param>
    /// <param name="clientId">The registered client.</param>
    /// <param name="grant">The identity-only scopes.</param>
    /// <param name="nonce">The nonce required and validated at authorization.</param>
    /// <returns>The identity stored in the protected authorization code.</returns>
    public static ClaimsIdentity Create(SignedInAccount authentication, string clientId, IdentityOnlyGrant grant, string nonce)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        identity.SetScopes(grant.Scopes);
        identity.SetResources(UserInfoAudience);
        identity.SetClaim(GrantMarker, "true");
        identity.SetClaim(Claims.Nonce, nonce);
        identity.SetClaim(SessionIdClaim, authentication.SessionId);
        identity.AddClaim(new Claim(Claims.AuthenticationTime, authentication.AuthTime.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64));
        Apply(identity, authentication.Account, clientId);
        return identity;
    }

    /// <summary>Renews an identity-only grant without changing its original authentication or session.</summary>
    /// <param name="granted">The protected code or refresh principal.</param>
    /// <param name="account">The current active canonical account.</param>
    /// <param name="clientId">The client redeeming its grant.</param>
    /// <param name="scopes">An optional validated narrowing of the original scope ceiling.</param>
    /// <returns>The renewed identity with current profile values and unchanged private grant metadata.</returns>
    public static ClaimsIdentity Renew(ClaimsPrincipal granted, Account account, string clientId, IEnumerable<string>? scopes = null)
    {
        var identity = new ClaimsIdentity(granted.Claims, TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        if (scopes is not null) identity.SetScopes(scopes);
        Apply(identity, account, clientId);
        return identity;
    }

    /// <summary>Recognizes only the private discriminator, never a client request parameter.</summary>
    /// <param name="principal">The principal read from a protected grant.</param>
    /// <returns>Whether the grant is identity-only.</returns>
    public static bool IsIdentityOnly(ClaimsPrincipal principal) => principal.GetClaim(GrantMarker) == "true";

    static void Apply(ClaimsIdentity identity, Account account, string clientId)
    {
        identity.SetClaim(Claims.Subject, account.Id.Value);
        identity.SetClaim(Claims.AuthorizedParty, clientId);
        SetProfile(identity, account);
        identity.SetDestinations(claim => DestinationsFor(claim.Type, identity.HasScope(Scopes.Profile)));
    }

    static void SetProfile(ClaimsIdentity identity, Account account)
    {
        var hasProfile = identity.HasScope(Scopes.Profile);
        identity.SetClaim(Claims.Name, hasProfile ? account.Name.Value : null);
        identity.SetClaim(Claims.PreferredUsername, hasProfile ? account.DisplayLogin()?.Value : null);
    }

    static IEnumerable<string> DestinationsFor(string type, bool hasProfile) => type switch
    {
        Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
        Claims.AuthenticationTime or Claims.Nonce or SessionIdClaim or Claims.AuthorizedParty => [Destinations.IdentityToken],
        Claims.Name or Claims.PreferredUsername when hasProfile => [Destinations.IdentityToken],
        _ => []
    };
}
