// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
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
/// <param name="timeProvider">The authentication-age clock.</param>
/// <param name="authenticationProof">The protected original-request binding for consent.</param>
internal class AuthorizationEndpoint(
    SignedInAccounts signedInAccounts,
    IGrantPolicy grantPolicy,
    ITenantDirectory tenants,
    ConsentPages consentPages,
    IOpenIddictApplicationManager applications,
    ILogger<AuthorizationEndpoint> logger,
    IOptions<IdentityOptions>? identityOptions = null,
    TimeProvider? timeProvider = null,
    OidcAuthenticationProof? authenticationProof = null)
{
    readonly IdentityOptions _options = identityOptions?.Value ?? new();
    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Handles an authorization request or a consent submission.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="IResult"/>.</returns>
    /// <exception cref="OpenIddictRequestMissing">Thrown when the endpoint is reached outside the OpenIddict pipeline.</exception>
    public async Task<IResult> Handle(HttpContext context)
    {
        var request = context.GetOpenIddictServerRequest() ?? throw new OpenIddictRequestMissing(context.Request.Path);
        // The GitHub callback returns here after an upstream cancel or failure. OpenIddict has revalidated
        // the client and redirect URI, so it can send a safe OAuth error instead of challenging again.
        if (context.Request.Query.ContainsKey("upstream_error"))
        {
            return context.Request.Query["upstream_error"] == "temporarily_unavailable"
                ? OAuthResults.Error(Errors.TemporarilyUnavailable, "Sign-in is temporarily unavailable. Try again later.")
                : OAuthResults.Error(Errors.AccessDenied, "Sign-in could not be completed.");
        }

        var signedIn = await signedInAccounts.Find(context, request);
        if (signedIn is not null && !HasAcceptableAuthentication(context, request, signedIn)) signedIn = null;
        if (signedIn is null)
        {
            return request.HasPromptValue(PromptValues.None)
                ? OAuthResults.Error(Errors.LoginRequired, "The user is not signed in.")
                : signedInAccounts.Challenge(context);
        }

        var evaluation = grantPolicy.Evaluate(new GrantRequest(request.GetScopes(), request.GetResources(), request.ClientId));
        if (evaluation is NotGrantable notGrantable)
        {
            return OAuthResults.Error(notGrantable.Error, notGrantable.Description);
        }

        if (evaluation is IdentityOnlyGrant identityGrant) return await AuthorizeIdentity(context, signedIn, identityGrant);

        return await AuthorizeResource(context, signedIn, ((Grantable)evaluation).Grant);
    }

    async Task<IResult> AuthorizeResource(HttpContext context, SignedInAccount signedIn, Grant grant)
    {
        var request = context.GetOpenIddictServerRequest()!;
        var account = signedIn.Account;
        var lookup = await tenants.SelectableTenants(account, grant.Resource, context.RequestAborted);
        if (lookup is not TenantsListed listed)
        {
            logger.TenantsUnavailable(account.Id, grant.Resource.Audience);
            return OAuthResults.Error(Errors.TemporarilyUnavailable, "Tenant selection is unavailable right now. Try again later.");
        }

        var selectable = listed.Tenants.ToArray();
        if (selectable.Length == 0)
        {
            logger.NoSelectableTenant(account.Id, grant.Resource.Audience);
            return OAuthResults.Error(Errors.AccessDenied, "The account has no tenant it can use with the requested resource.");
        }

        var decision = IsConsentSubmission(context) ? context.Request.Form[ConsentForm.Decision].ToString() : null;
        if (decision is null)
        {
            return request.HasPromptValue(PromptValues.None)
                ? OAuthResults.Error(Errors.ConsentRequired, "The user must approve the request interactively.")
                : consentPages.Render(context, await ConsentFor(context, account, grant, selectable));
        }
        return await ApproveResource(context, signedIn, grant, selectable);
    }

    async Task<IResult> ApproveResource(HttpContext context, SignedInAccount signedIn, Grant grant, SelectableTenant[] selectable)
    {
        var request = context.GetOpenIddictServerRequest()!;
        var clientId = request.ClientId!;
        var account = signedIn.Account;
        var decision = context.Request.Form[ConsentForm.Decision].ToString();
        if (!await consentPages.IsValidSubmission(context))
        {
            return Results.BadRequest("The consent form expired or did not come from this browser session. Start the sign-in again.");
        }

        if (!string.Equals(decision, ConsentForm.Allow, StringComparison.Ordinal))
        {
            logger.ConsentDenied(account.Id, clientId);
            return OAuthResults.Error(Errors.AccessDenied, "The user denied the request.");
        }

        var tenantId = context.Request.Form[ConsentForm.Tenant].ToString();
        var tenant = selectable.FirstOrDefault(_ => string.Equals(_.Id, tenantId, StringComparison.Ordinal));
        if (tenant is null)
        {
            var consent = await ConsentFor(context, account, grant, selectable);
            return consentPages.Render(context, consent with { Error = "Select one of the listed tenants." });
        }

        logger.ConsentGranted(account.Id, clientId, grant.Resource.Audience, tenant.Id, string.Join(' ', grant.Scopes));
        return OAuthResults.Issue(AccessTokenIdentity.Create(account, clientId, grant, tenant, signedIn.AuthTime));
    }

    async Task<IResult> AuthorizeIdentity(HttpContext context, SignedInAccount signedIn, IdentityOnlyGrant grant)
    {
        var request = context.GetOpenIddictServerRequest()!;
        if (!_options.Oidc.IssuanceEnabled) return OAuthResults.Error(Errors.TemporarilyUnavailable, "OpenID Connect issuance is disabled.");
        if (request.HasPromptValue(PromptValues.None)) return OAuthResults.Error(Errors.ConsentRequired, "The user must approve the request interactively.");
        if (!IsConsentSubmission(context)) return RenderIdentity(context, signedIn, grant);
        if (!await consentPages.IsValidSubmission(context)) return Results.BadRequest("The consent form expired or did not come from this browser session. Start the sign-in again.");
        if (context.Request.Form[ConsentForm.Decision] != ConsentForm.Allow) return OAuthResults.Error(Errors.AccessDenied, "The user denied the request.");
        return OAuthResults.Issue(OidcIdentity.Create(signedIn, request.ClientId!, grant, request.Nonce!));
    }

    bool HasAcceptableAuthentication(HttpContext context, OpenIddictRequest request, SignedInAccount signedIn) =>
        OidcAuthentication.IsFresh(request, signedIn.AuthTime, _time.GetUtcNow()) || authenticationProof?.IsValid(context, signedIn) == true;

    IResult RenderIdentity(HttpContext context, SignedInAccount signedIn, IdentityOnlyGrant grant)
    {
        if (authenticationProof is null) return OAuthResults.Error(Errors.TemporarilyUnavailable, "OpenID Connect authentication binding is unavailable.");
        var request = context.GetOpenIddictServerRequest()!;
        var client = _options.OidcClients.Single(client => client.ClientId == request.ClientId);
        var parameters = AuthorizationParameters.From(context.Request).Where(parameter => parameter.Key != OidcAuthenticationProof.Field)
            .Append(new(OidcAuthenticationProof.Field, authenticationProof.Issue(context, signedIn)));
        return consentPages.Render(context, new IdentityConsent(client.DisplayName ?? client.ClientId, new Uri(request.RedirectUri!, UriKind.Absolute), grant, parameters));
    }

    static bool IsConsentSubmission(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method) && context.Request.HasFormContentType && context.Request.Form.ContainsKey(ConsentForm.Decision);

    async Task<Consent> ConsentFor(HttpContext context, Account account, Grant grant, IEnumerable<SelectableTenant> selectable)
    {
        var request = context.GetOpenIddictServerRequest()!;
        var application = await applications.FindByClientIdAsync(request.ClientId!, context.RequestAborted) ?? throw new ClientNotRegistered(request.ClientId!);
        var name = await applications.GetDisplayNameAsync(application, context.RequestAborted) ?? request.ClientId!;
        return new Consent(
            request.ClientId!,
            name,
            new Uri(request.RedirectUri!),
            grant,
            selectable,
            account,
            AuthorizationParameters.From(context.Request));
    }
}
