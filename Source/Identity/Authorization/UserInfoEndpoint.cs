// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Returns current active-account data only for a fenced, validated identity-only access token.</summary>
/// <param name="accounts">The active-account reader.</param>
/// <param name="options">The validated issuance gate.</param>
internal sealed class UserInfoEndpoint(IAccountReader accounts, IOptions<IdentityOptions> options)
{
    /// <summary>Handles a GET or POST after native UserInfo access-token validation and existing fence handlers.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>The bounded account projection, or an invalid_token challenge.</returns>
    public async Task<IResult> Handle(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        if (!options.Value.Oidc.IssuanceEnabled) return Results.NotFound();
        var principal = await ValidatedIdentityToken(context);
        if (principal is null) return Invalid(context);
        if (!AccountSubject.TryParse(principal.GetClaim(Claims.Subject), out var subject)) return Invalid(context);
        var account = await accounts.FindActive(subject!, context.RequestAborted);
        return account is null ? Invalid(context) : Results.Json(UserInfoClaims.For(account, principal.HasScope(Scopes.Profile)));
    }

    static async Task<ClaimsPrincipal?> ValidatedIdentityToken(HttpContext context)
    {
        if (!context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal is null) return null;
        var principal = result.Principal;
        if (principal.GetTokenType() != TokenTypeIdentifiers.AccessToken || !principal.HasScope(Scopes.OpenId)) return null;
        return principal.GetAudiences().SequenceEqual([OidcIdentity.UserInfoAudience], StringComparer.Ordinal) ? principal : null;
    }

    static IResult Invalid(HttpContext context)
    {
        context.Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\"";
        return Results.Json(new { error = Errors.InvalidToken }, statusCode: StatusCodes.Status401Unauthorized);
    }
}
