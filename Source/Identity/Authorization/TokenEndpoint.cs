// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Device;
using Cratis.Identity.Resources;
using Cratis.Identity.Revocation;
using Cratis.Identity.Tenants;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
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
/// <param name="accounts">The active-account reader.</param>
/// <param name="grantPolicy">The <see cref="IGrantPolicy"/>.</param>
/// <param name="resources">The <see cref="IResourceCatalog"/>.</param>
/// <param name="tenants">The <see cref="ITenantDirectory"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="fences">The primary grant authority.</param>
/// <param name="deviceAdmission">The device recovery and epoch checks.</param>
/// <param name="exchange">The RFC 8693 <see cref="TokenExchangeGrant"/>.</param>
/// <param name="identityOptions">The validated issuance configuration.</param>
internal class TokenEndpoint(
    IAccountReader accounts,
    IGrantPolicy grantPolicy,
    IResourceCatalog resources,
    ITenantDirectory tenants,
    ILogger<TokenEndpoint> logger,
    IFenceStore fences,
    DeviceGrantAdmission deviceAdmission,
    TokenExchangeGrant exchange,
    IOptions<IdentityOptions>? identityOptions = null)
{
    readonly IdentityOptions _options = identityOptions?.Value ?? new();
    /// <summary>
    /// Handles a token request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="IResult"/>.</returns>
    /// <exception cref="OpenIddictRequestMissing">Thrown when the endpoint is reached outside the OpenIddict pipeline.</exception>
    public async Task<IResult> Handle(HttpContext context)
    {
        var request = context.GetOpenIddictServerRequest() ?? throw new OpenIddictRequestMissing(context.Request.Path);
        if (string.Equals(request.GrantType, TokenExchangeConstants.GrantType, StringComparison.Ordinal))
        {
            return await exchange.Handle(request, context.RequestAborted);
        }

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType() && !request.IsDeviceCodeGrantType())
        {
            return OAuthResults.Error(Errors.UnsupportedGrantType, "The grant type is not supported.");
        }

        return await Redeem(context, request);
    }

    async Task<IResult> Redeem(HttpContext context, OpenIddictRequest request)
    {
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!await DeviceGrantIsValid(request, result, context.RequestAborted))
        {
            return OAuthResults.Error(Errors.InvalidGrant, "The device authorization is no longer valid.");
        }

        if (!result.Succeeded || result.Principal is null) return OAuthResults.Error(Errors.InvalidGrant, "The grant is no longer valid.");
        var granted = result.Principal;
        if (ValidateBinding(granted, request) is { } bindingRejection) return bindingRejection;
        var authorizationId = granted.GetAuthorizationId();

        var accountId = granted.GetClaim(Claims.Subject);
        var account = AccountSubject.TryParse(accountId, out var subject) ? await accounts.FindActive(subject!, context.RequestAborted) : null;
        if (account is null)
        {
            logger.AccountMissing(authorizationId, accountId);
            return OAuthResults.Error(Errors.InvalidGrant, "The account this grant belongs to no longer exists.");
        }

        if (await ValidateRenewal(context, request, granted, account) is { } renewalRejection) return renewalRejection;
        return IssueRenewed(granted, account, request);
    }

    async Task<bool> DeviceGrantIsValid(OpenIddictRequest request, AuthenticateResult result, CancellationToken cancellationToken)
    {
        if (!request.IsDeviceCodeGrantType()) return true;
        if (!result.Succeeded || result.Principal is null) return false;
        return await deviceAdmission.CanUse(result.Principal, true, cancellationToken);
    }

    IResult? ValidateBinding(ClaimsPrincipal granted, OpenIddictRequest request)
    {
        if (OidcIdentity.IsIdentityOnly(granted))
        {
            return IdentityGrantAdmission.CanRedeem(granted, request, _options) ? null : OAuthResults.Error(Errors.InvalidGrant, "The identity grant is no longer valid.");
        }
        var rejection = grantPolicy.EvaluateTokenRequest(request.GetResources(), granted.GetResources());
        return rejection is null ? null : OAuthResults.Error(rejection.Error, rejection.Description);
    }

    async Task<IResult?> ValidateRenewal(HttpContext context, OpenIddictRequest request, ClaimsPrincipal granted, Account account)
    {
        if (!OidcIdentity.IsIdentityOnly(granted) && await ValidateResources(context, granted, account) is { } resourceRejection) return resourceRejection;
        if (!await RefreshStateIsValid(granted, request, context.RequestAborted)) return OAuthResults.Error(Errors.InvalidGrant, "The refresh token is no longer valid.");
        // Authentication time is never invented from the current time.
        return AccessTokenIdentity.AuthTimeOf(granted) is null ? OAuthResults.Error(Errors.InvalidGrant, "The refresh token is no longer valid.") : null;
    }

    async Task<bool> RefreshStateIsValid(ClaimsPrincipal granted, OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var authorizationId = granted.GetAuthorizationId();
        if (string.IsNullOrEmpty(authorizationId)) return false;
        try
        {
            var state = await fences.Read(authorizationId, cancellationToken);
            if (state is null) return !request.IsRefreshTokenGrantType();
            // A known non-current entry must reach redemption so reuse fences the family.
            return state.Fence is null && GrantStateValidation.IsKnown(state);
        }
        catch (Exception)
        {
            return false;
        }
    }

    IResult IssueRenewed(ClaimsPrincipal granted, Account account, OpenIddictRequest request)
    {
        if (OidcIdentity.IsIdentityOnly(granted))
        {
            var requestedScopes = request.GetScopes();
            return OAuthResults.Issue(OidcIdentity.Renew(granted, account, request.ClientId!, requestedScopes.Length > 0 ? requestedScopes : null));
        }

        var receivesUpstreamIdentity = granted.GetResources().All(_ => resources.FindByAudience(_)?.ReceivesUpstreamIdentity == true);
        return OAuthResults.Issue(AccessTokenIdentity.Renew(granted, account, request.ClientId!, receivesUpstreamIdentity));
    }

    async Task<IResult?> ValidateResources(HttpContext context, ClaimsPrincipal granted, Account account)
    {
        var grantedAudiences = granted.GetResources();
        if (grantedAudiences.Length == 0) return OAuthResults.Error(Errors.InvalidGrant, "The grant is not bound to a resource.");
        var tenantId = granted.GetClaim(IdentityClaims.TenantId);
        foreach (var resource in grantedAudiences.Select(resources.FindByAudience))
        {
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
        }
        return null;
    }
}
