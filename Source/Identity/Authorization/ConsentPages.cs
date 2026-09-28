// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Antiforgery;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Serves the consent page and validates its submissions.
/// </summary>
/// <param name="antiforgery">The <see cref="IAntiforgery"/>.</param>
public class ConsentPages(IAntiforgery antiforgery)
{
    /// <summary>
    /// Renders the consent page for a request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="consent">The <see cref="Consent"/> to render.</param>
    /// <returns>The <see cref="IResult"/>.</returns>
    public IResult Render(HttpContext context, Consent consent)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        var headers = context.Response.Headers;
        headers.CacheControl = "no-store";

        // form-action must allow the redirect URI too: browsers apply it to the redirect that follows the post.
        var redirectOrigin = consent.RedirectUri.GetLeftPart(UriPartial.Authority);
        headers.ContentSecurityPolicy =
            $"default-src 'none'; style-src 'unsafe-inline'; form-action 'self' {redirectOrigin}; frame-ancestors 'none'; base-uri 'none'";

        return Results.Content(ConsentPage.Render(consent, tokens.RequestToken!), "text/html; charset=utf-8");
    }

    /// <summary>
    /// Validates that a consent submission came from a consent page Identity served to the same browser session.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns><see langword="true"/> if the submission is valid.</returns>
    public Task<bool> IsValidSubmission(HttpContext context) => antiforgery.IsRequestValidAsync(context);
}
