// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Applies authentication-age and explicit-consent checks to identity-only authorization.</summary>
/// <param name="options">The validated issuance configuration.</param>
/// <param name="consentPages">The browser-bound consent pages.</param>
/// <param name="time">The authentication-age clock.</param>
/// <param name="authenticationProof">The protected original-request binding for consent.</param>
internal class OidcAuthorization(IdentityOptions options, ConsentPages consentPages, TimeProvider time, OidcAuthenticationProof? authenticationProof)
{
    /// <summary>Requires fresh authentication or the exact protected consent continuation.</summary>
    /// <param name="context">The current request.</param>
    /// <param name="request">The validated protocol request.</param>
    /// <param name="signedIn">The original upstream authentication.</param>
    /// <returns>Whether that authentication can continue.</returns>
    public bool HasAcceptableAuthentication(HttpContext context, OpenIddictRequest request, SignedInAccount signedIn) =>
        OidcAuthentication.IsFresh(request, signedIn.AuthTime, time.GetUtcNow()) || authenticationProof?.IsValid(context, signedIn) == true;

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
        if (!ConsentSubmission.IsSubmitted(context)) return Render(context, signedIn, grant);
        return await Approve(context, signedIn, grant);
    }

    async Task<IResult> Approve(HttpContext context, SignedInAccount signedIn, IdentityOnlyGrant grant)
    {
        if (!await consentPages.IsValidSubmission(context)) return Results.BadRequest("The consent form expired or did not come from this browser session. Start the sign-in again.");
        if (context.Request.Form[ConsentForm.Decision] != ConsentForm.Allow) return OAuthResults.Error(Errors.AccessDenied, "The user denied the request.");
        var request = context.GetOpenIddictServerRequest()!;
        return OAuthResults.Issue(OidcIdentity.Create(signedIn, request.ClientId!, grant, request.Nonce!));
    }

    IResult Render(HttpContext context, SignedInAccount signedIn, IdentityOnlyGrant grant)
    {
        if (authenticationProof is null) return OAuthResults.Error(Errors.TemporarilyUnavailable, "OpenID Connect authentication binding is unavailable.");
        var request = context.GetOpenIddictServerRequest()!;
        var client = options.OidcClients.Single(client => client.ClientId == request.ClientId);
        var parameters = AuthorizationParameters.From(context.Request).Where(parameter => parameter.Key != OidcAuthenticationProof.Field)
            .Append(new(OidcAuthenticationProof.Field, authenticationProof.Issue(context, signedIn)));
        return consentPages.Render(context, new IdentityConsent(client.DisplayName ?? client.ClientId, new Uri(request.RedirectUri!, UriKind.Absolute), grant, parameters));
    }
}
