// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.Device;
using Cratis.Identity.Resources;
using Cratis.Identity.Revocation;
using Cratis.Identity.Tenants;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
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
    readonly TokenGrantRedemption _redemption = new(
        accounts,
        deviceAdmission,
        new TokenGrantRenewal(identityOptions?.Value ?? new(), new ResourceTokenGrant(grantPolicy, resources, tenants, logger), new RefreshGrantAdmission(fences)),
        logger);

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

        if (!SupportsRedemption(request)) return OAuthResults.Error(Errors.UnsupportedGrantType, "The grant type is not supported.");
        return await _redemption.Redeem(context, request);
    }

    static bool SupportsRedemption(OpenIddictRequest request)
    {
        if (request.IsAuthorizationCodeGrantType()) return true;
        if (request.IsRefreshTokenGrantType()) return true;
        return request.IsDeviceCodeGrantType();
    }
}
