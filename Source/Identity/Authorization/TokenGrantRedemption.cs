// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Device;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Admits an authenticated code, refresh token or device grant before renewing its identity.</summary>
/// <param name="accounts">The active-account reader.</param>
/// <param name="deviceAdmission">The device recovery and epoch checks.</param>
/// <param name="renewal">The protected grant binding and renewal policy.</param>
/// <param name="logger">The existing token audit logger.</param>
internal class TokenGrantRedemption(IAccountReader accounts, DeviceGrantAdmission deviceAdmission, TokenGrantRenewal renewal, ILogger<TokenEndpoint> logger)
{
    /// <summary>Authenticates the native token and applies device admission before reading its principal.</summary>
    /// <param name="context">The current request.</param>
    /// <param name="request">The validated token request.</param>
    /// <returns>The token issuance or rejection result.</returns>
    public async Task<IResult> Redeem(HttpContext context, OpenIddictRequest request)
    {
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!await DeviceGrantIsValid(request, result, context.RequestAborted))
        {
            return OAuthResults.Error(Errors.InvalidGrant, "The device authorization is no longer valid.");
        }

        if (!result.Succeeded || result.Principal is null) return OAuthResults.Error(Errors.InvalidGrant, "The grant is no longer valid.");
        return await RedeemPrincipal(context, request, result.Principal);
    }

    async Task<IResult> RedeemPrincipal(HttpContext context, OpenIddictRequest request, ClaimsPrincipal granted)
    {
        if (renewal.ValidateBinding(granted, request) is { } bindingRejection) return bindingRejection;
        var authorizationId = granted.GetAuthorizationId();
        var accountId = granted.GetClaim(Claims.Subject);
        var account = await FindAccount(accountId, context.RequestAborted);
        if (account is null)
        {
            logger.AccountMissing(authorizationId, accountId);
            return OAuthResults.Error(Errors.InvalidGrant, "The account this grant belongs to no longer exists.");
        }

        if (await renewal.Validate(context, request, granted, account) is { } renewalRejection) return renewalRejection;
        return renewal.Issue(granted, account, request);
    }

    async Task<Account?> FindAccount(string? accountId, CancellationToken cancellationToken) =>
        AccountSubject.TryParse(accountId, out var subject) ? await accounts.FindActive(subject!, cancellationToken) : null;

    async Task<bool> DeviceGrantIsValid(OpenIddictRequest request, AuthenticateResult result, CancellationToken cancellationToken)
    {
        if (!request.IsDeviceCodeGrantType()) return true;
        if (!result.Succeeded || result.Principal is null) return false;
        return await deviceAdmission.CanUse(result.Principal, true, cancellationToken);
    }
}
