// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Configuration;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Keeps identity-only renewal separate from resource checks while sharing refresh admission.</summary>
/// <param name="options">The validated issuance configuration.</param>
/// <param name="resources">The resource grant checks and renewal.</param>
/// <param name="refresh">The existing refresh-state admission checks.</param>
internal class TokenGrantRenewal(IdentityOptions options, ResourceTokenGrant resources, RefreshGrantAdmission refresh)
{
    /// <summary>Checks the protected identity or resource binding before reading account data.</summary>
    /// <param name="granted">The validated token principal.</param>
    /// <param name="request">The validated token request.</param>
    /// <returns>A rejection, or no rejection.</returns>
    public IResult? ValidateBinding(ClaimsPrincipal granted, OpenIddictRequest request)
    {
        if (!OidcIdentity.IsIdentityOnly(granted)) return resources.ValidateBinding(granted, request);
        return IdentityGrantAdmission.CanRedeem(granted, request, options) ? null : OAuthResults.Error(Errors.InvalidGrant, "The identity grant is no longer valid.");
    }

    /// <summary>Rechecks resource access when applicable, refresh state, and the original authentication time.</summary>
    /// <param name="context">The current request.</param>
    /// <param name="request">The validated token request.</param>
    /// <param name="granted">The validated token principal.</param>
    /// <param name="account">The current active account.</param>
    /// <returns>A rejection, or no rejection.</returns>
    public async Task<IResult?> Validate(HttpContext context, OpenIddictRequest request, ClaimsPrincipal granted, Account account)
    {
        if (await ValidateResources(context, granted, account) is { } rejection) return rejection;
        if (!await refresh.IsValid(granted, request, context.RequestAborted)) return OAuthResults.Error(Errors.InvalidGrant, "The refresh token is no longer valid.");
        // Authentication time is never invented from the current time.
        return AccessTokenIdentity.AuthTimeOf(granted) is null ? OAuthResults.Error(Errors.InvalidGrant, "The refresh token is no longer valid.") : null;
    }

    /// <summary>Renews only the claim contract belonging to the protected grant kind.</summary>
    /// <param name="granted">The validated token principal.</param>
    /// <param name="account">The current active account.</param>
    /// <param name="request">The validated token request.</param>
    /// <returns>The token issuance result.</returns>
    public IResult Issue(ClaimsPrincipal granted, Account account, OpenIddictRequest request)
    {
        if (!OidcIdentity.IsIdentityOnly(granted)) return resources.Issue(granted, account, request);
        var requestedScopes = request.GetScopes();
        return OAuthResults.Issue(OidcIdentity.Renew(granted, account, request.ClientId!, requestedScopes.Length > 0 ? requestedScopes : null));
    }

    Task<IResult?> ValidateResources(HttpContext context, ClaimsPrincipal granted, Account account) =>
        OidcIdentity.IsIdentityOnly(granted) ? Task.FromResult<IResult?>(null) : resources.Validate(context, granted, account);
}
