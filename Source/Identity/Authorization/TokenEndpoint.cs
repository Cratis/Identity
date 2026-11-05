// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Handles authorization-code redemption and refresh after OpenIddict has validated the token itself (PKCE,
/// expiry, redemption state and refresh-token reuse). Every token request re-checks that the account still
/// exists, that the requested resource matches the grant, and that the account can still use the tenant the
/// grant was authorized for.
/// </summary>
/// <param name="accounts">The <see cref="IAccounts"/>.</param>
/// <param name="grantPolicy">The <see cref="IGrantPolicy"/>.</param>
/// <param name="resources">The <see cref="IResourceCatalog"/>.</param>
/// <param name="tenants">The <see cref="ITenantDirectory"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class TokenEndpoint(
    IAccounts accounts,
    IGrantPolicy grantPolicy,
    IResourceCatalog resources,
    ITenantDirectory tenants,
    ILogger<TokenEndpoint> logger)
{
    /// <summary>
    /// Handles a token request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="IResult"/>.</returns>
    /// <exception cref="OpenIddictRequestMissing">Thrown when the endpoint is reached outside the OpenIddict pipeline.</exception>
    public async Task<IResult> Handle(HttpContext context)
    {
        var request = context.GetOpenIddictServerRequest() ?? throw new OpenIddictRequestMissing(context.Request.Path);
        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            return OAuthResults.Error(Errors.UnsupportedGrantType, "The grant type is not supported.");
        }

        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var granted = result.Principal!;
        var authorizationId = granted.GetAuthorizationId();

        if (grantPolicy.EvaluateTokenRequest(request.GetResources(), granted.GetResources()) is { } rejection)
        {
            return OAuthResults.Error(rejection.Error, rejection.Description);
        }

        var accountId = granted.GetClaim(Claims.Subject);
        var account = accountId is null ? null : await accounts.FindById(accountId, context.RequestAborted);
        if (account is null)
        {
            logger.AccountMissing(authorizationId, accountId);
            return OAuthResults.Error(Errors.InvalidGrant, "The account this grant belongs to no longer exists.");
        }

        var grantedAudiences = granted.GetResources();
        if (grantedAudiences.Length == 0)
        {
            return OAuthResults.Error(Errors.InvalidGrant, "The grant is not bound to a resource.");
        }

        var tenantId = granted.GetClaim(IdentityClaims.TenantId);
        foreach (var resource in grantedAudiences.Select(resources.FindByAudience))
        {
            var lookup = resource is null ? new TenantsListed([]) : await tenants.SelectableTenants(account, resource, context.RequestAborted);
            if (lookup is not TenantsListed listed)
            {
                logger.TenantsUnavailable(authorizationId, account.Id, resource!.Audience);
                return OAuthResults.Error(Errors.TemporarilyUnavailable, "Tenant selection is unavailable right now. Try again later.");
            }

            var selectable = listed.Tenants;
            if (!selectable.Any(_ => string.Equals(_.Id, tenantId, StringComparison.Ordinal)))
            {
                logger.TenantNoLongerSelectable(authorizationId, account.Id, tenantId, resource?.Audience ?? "(unknown)");
                return OAuthResults.Error(Errors.InvalidGrant, "The account can no longer use the tenant this grant was authorized for.");
            }
        }

        return OAuthResults.Issue(AccessTokenIdentity.Renew(granted, account, request.ClientId!));
    }
}
