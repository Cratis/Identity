// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Handles the authorization endpoint after OpenIddict has validated the protocol (client, redirect URI, PKCE,
/// scopes and resource indicators): upstream login, grant evaluation, tenant selection and explicit consent.
/// </summary>
/// <remarks>
/// Consent is asked for on every authorization in v1; there is no remembered consent, so <c language="text">prompt=none</c>
/// always fails with <c language="text">consent_required</c> or <c language="text">login_required</c>.
/// </remarks>
/// <param name="signedInAccounts">The <see cref="SignedInAccounts"/>.</param>
/// <param name="grantPolicy">The <see cref="IGrantPolicy"/>.</param>
/// <param name="tenants">The <see cref="ITenantDirectory"/>.</param>
/// <param name="consentPages">The <see cref="ConsentPages"/>.</param>
/// <param name="applications">The <see cref="IOpenIddictApplicationManager"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="identityOptions">The validated issuance configuration.</param>
internal class AuthorizationEndpoint(
    SignedInAccounts signedInAccounts,
    IGrantPolicy grantPolicy,
    ITenantDirectory tenants,
    ConsentPages consentPages,
    IOpenIddictApplicationManager applications,
    ILogger<AuthorizationEndpoint> logger,
    IOptions<IdentityOptions>? identityOptions = null)
{
    readonly OidcAuthorization _identity = new(identityOptions?.Value ?? new(), consentPages);
    readonly ResourceAuthorization _resource = new(tenants, consentPages, applications, logger);

    /// <summary>
    /// Handles an authorization request or a consent submission.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="IResult"/>.</returns>
    /// <exception cref="OpenIddictRequestMissing">Thrown when the endpoint is reached outside the OpenIddict pipeline.</exception>
    public async Task<IResult> Handle(HttpContext context)
    {
        var request = context.GetOpenIddictServerRequest() ?? throw new OpenIddictRequestMissing(context.Request.Path);
        if (await MaxAgeRequest.IsPresent(context.Request)) return OAuthResults.Error(Errors.InvalidRequest, "max_age is not supported");
        if (request.HasPromptValue(PromptValues.None) && request.GetPromptValues().Length > 1) return OAuthResults.Error(Errors.InvalidRequest, "prompt none cannot be combined with other values");

        // The GitHub callback returns here after an upstream cancel or failure. OpenIddict has revalidated
        // the client and redirect URI, so it can send a safe OAuth error instead of challenging again.
        if (context.Request.Query.ContainsKey("upstream_error")) return UpstreamFailure(context);

        var signedIn = await signedInAccounts.Find(context, request);
        if (signedIn is null) return RequireLogin(context, request);
        return await Authorize(context, request, signedIn);
    }

    static IResult UpstreamFailure(HttpContext context) =>
        context.Request.Query["upstream_error"] == "temporarily_unavailable"
            ? OAuthResults.Error(Errors.TemporarilyUnavailable, "Sign-in is temporarily unavailable. Try again later.")
            : OAuthResults.Error(Errors.AccessDenied, "Sign-in could not be completed.");

    IResult RequireLogin(HttpContext context, OpenIddictRequest request) =>
        request.HasPromptValue(PromptValues.None)
            ? OAuthResults.Error(Errors.LoginRequired, "The user is not signed in.")
            : signedInAccounts.Challenge(context);

    async Task<IResult> Authorize(HttpContext context, OpenIddictRequest request, SignedInAccount signedIn)
    {
        var evaluation = grantPolicy.Evaluate(new GrantRequest(request.GetScopes(), request.GetResources(), request.ClientId));
        if (evaluation is NotGrantable notGrantable) return OAuthResults.Error(notGrantable.Error, notGrantable.Description);
        if (evaluation is IdentityOnlyGrant identityGrant) return await _identity.Authorize(context, signedIn, identityGrant);
        return await _resource.Authorize(context, signedIn, ((Grantable)evaluation).Grant);
    }
}
