// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Rechecks resource binding and tenant access before renewing a resource grant.</summary>
/// <param name="grantPolicy">The resource-indicator policy.</param>
/// <param name="resources">The registered resource catalog.</param>
/// <param name="tenants">The account's selectable tenants.</param>
/// <param name="logger">The existing token audit logger.</param>
internal class ResourceTokenGrant(IGrantPolicy grantPolicy, IResourceCatalog resources, ITenantDirectory tenants, ILogger<TokenEndpoint> logger)
{
    /// <summary>Checks that the request does not change the resource binding.</summary>
    /// <param name="granted">The validated token principal.</param>
    /// <param name="request">The validated token request.</param>
    /// <returns>A rejection, or no rejection.</returns>
    public IResult? ValidateBinding(ClaimsPrincipal granted, OpenIddictRequest request)
    {
        var rejection = grantPolicy.EvaluateTokenRequest(request.GetResources(), granted.GetResources());
        return rejection is null ? null : OAuthResults.Error(rejection.Error, rejection.Description);
    }

    /// <summary>Rechecks current tenant access for every granted resource.</summary>
    /// <param name="context">The current request.</param>
    /// <param name="granted">The validated token principal.</param>
    /// <param name="account">The current active account.</param>
    /// <returns>A rejection, or no rejection.</returns>
    public async Task<IResult?> Validate(HttpContext context, ClaimsPrincipal granted, Account account)
    {
        var grantedAudiences = granted.GetResources();
        if (grantedAudiences.Length == 0) return OAuthResults.Error(Errors.InvalidGrant, "The grant is not bound to a resource.");
        foreach (var resource in grantedAudiences.Select(resources.FindByAudience))
        {
            if (await ValidateTenant(context, granted, account, resource) is { } rejection) return rejection;
        }
        return null;
    }

    /// <summary>Renews resource claims with the existing upstream-identity destination policy.</summary>
    /// <param name="granted">The validated token principal.</param>
    /// <param name="account">The current active account.</param>
    /// <param name="request">The validated token request.</param>
    /// <returns>The token issuance result.</returns>
    public IResult Issue(ClaimsPrincipal granted, Account account, OpenIddictRequest request)
    {
        var receivesUpstreamIdentity = granted.GetResources().All(_ => resources.FindByAudience(_)?.ReceivesUpstreamIdentity == true);
        return OAuthResults.Issue(AccessTokenIdentity.Renew(granted, account, request.ClientId!, receivesUpstreamIdentity));
    }

    async Task<IResult?> ValidateTenant(HttpContext context, ClaimsPrincipal granted, Account account, ResourceServer? resource)
    {
        var tenantId = granted.GetClaim(IdentityClaims.TenantId);
        var lookup = resource is null ? new TenantsListed([]) : await tenants.SelectableTenants(account, resource, context.RequestAborted);
        if (lookup is not TenantsListed listed)
        {
            logger.TenantsUnavailable(granted.GetAuthorizationId(), account.Id, resource!.Audience);
            return OAuthResults.Error(Errors.TemporarilyUnavailable, "Tenant selection is unavailable right now. Try again later.");
        }
        if (!listed.Tenants.Any(tenant => string.Equals(tenant.Id, tenantId, StringComparison.Ordinal)))
        {
            logger.TenantNoLongerSelectable(granted.GetAuthorizationId(), account.Id, tenantId, resource?.Audience ?? "(unknown)");
            return OAuthResults.Error(Errors.InvalidGrant, "The account can no longer use the tenant this grant was authorized for.");
        }
        return null;
    }
}
