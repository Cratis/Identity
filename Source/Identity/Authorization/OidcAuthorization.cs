// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Applies explicit-consent checks to identity-only authorization.</summary>
/// <param name="options">The validated issuance configuration.</param>
/// <param name="consentPages">The browser-bound consent pages.</param>
internal class OidcAuthorization(IdentityOptions options, ConsentPages consentPages)
{
    /// <summary>Renders or admits an identity-only consent decision.</summary>
    /// <param name="context">The current request.</param>
    /// <param name="signedIn">The original upstream authentication.</param>
    /// <param name="grant">The identity-only scope grant.</param>
    /// <returns>The consent page, rejection, or token issuance result.</returns>
    public async Task<IResult> Authorize(HttpContext context, SignedInAccount signedIn, IdentityOnlyGrant grant)
    {
        var request = context.GetOpenIddictServerRequest()!;
        if (!options.Oidc.IssuanceEnabled) return OAuthResults.Error(Errors.TemporarilyUnavailable, "OpenID Connect issuance is disabled.");
        if (request.HasPromptValue(PromptValues.None)) return OAuthResults.Error(Errors.ConsentRequired, "The user must approve the request interactively.");
        if (!ConsentSubmission.IsSubmitted(context)) return Render(context, grant);
        return await Approve(context, signedIn, grant);
    }

    async Task<IResult> Approve(HttpContext context, SignedInAccount signedIn, IdentityOnlyGrant grant)
    {
        if (!await consentPages.IsValidSubmission(context)) return Results.BadRequest("The consent form expired or did not come from this browser session. Start the sign-in again.");
        if (context.Request.Form[ConsentForm.Decision] != ConsentForm.Allow) return OAuthResults.Error(Errors.AccessDenied, "The user denied the request.");
        var request = context.GetOpenIddictServerRequest()!;
        return OAuthResults.Issue(OidcIdentity.Create(signedIn, request.ClientId!, grant, request.Nonce!));
    }

    IResult Render(HttpContext context, IdentityOnlyGrant grant)
    {
        var request = context.GetOpenIddictServerRequest()!;
        var client = options.OidcClients.Single(client => client.ClientId == request.ClientId);
        var parameters = AuthorizationParameters.From(context.Request);
        return consentPages.Render(context, new IdentityConsent(client.DisplayName ?? client.ClientId, new Uri(request.RedirectUri!, UriKind.Absolute), grant, parameters));
    }
}
