// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Applies tenant selection and explicit consent to resource authorization.</summary>
/// <param name="tenants">The account's selectable tenants.</param>
/// <param name="consentPages">The browser-bound consent pages.</param>
/// <param name="applications">The registered client display names.</param>
/// <param name="logger">The existing authorization audit logger.</param>
internal class ResourceAuthorization(ITenantDirectory tenants, ConsentPages consentPages, IOpenIddictApplicationManager applications, ILogger<AuthorizationEndpoint> logger)
{
    /// <summary>Renders or admits a resource consent decision.</summary>
    /// <param name="context">The current request.</param>
    /// <param name="signedIn">The original upstream authentication.</param>
    /// <param name="grant">The evaluated resource grant.</param>
    /// <returns>The consent page, rejection, or token issuance result.</returns>
    public async Task<IResult> Authorize(HttpContext context, SignedInAccount signedIn, Grant grant)
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

        if (!ConsentSubmission.IsSubmitted(context))
        {
            return request.HasPromptValue(PromptValues.None)
                ? OAuthResults.Error(Errors.ConsentRequired, "The user must approve the request interactively.")
                : consentPages.Render(context, await ConsentFor(context, account, grant, selectable));
        }
        return await Approve(context, signedIn, grant, selectable);
    }

    async Task<IResult> Approve(HttpContext context, SignedInAccount signedIn, Grant grant, SelectableTenant[] selectable)
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
