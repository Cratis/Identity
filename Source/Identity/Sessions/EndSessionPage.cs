// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Antiforgery;

namespace Cratis.Identity.Sessions;

/// <summary>Renders the browser-session logout confirmation without reflecting untrusted markup.</summary>
internal static class EndSessionPage
{
    /// <summary>Renders a confirmation form. Only a validated redirect may be retained.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="antiforgery">The antiforgery service.</param>
    /// <param name="request">The parameters to retain for the confirmed POST.</param>
    /// <param name="invalidRedirect">Whether an invalid redirect was discarded.</param>
    /// <returns>The confirmation page.</returns>
    public static IResult Confirm(HttpContext context, IAntiforgery antiforgery, EndSessionRequest request, bool invalidRedirect = false)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        var action = $"{context.Request.PathBase}{context.Request.Path}";
        var redirectOrigin = request.RedirectUri is null ? string.Empty : new Uri(request.RedirectUri).GetLeftPart(UriPartial.Authority);
        context.Response.Headers.ContentSecurityPolicy = $"default-src 'none'; form-action 'self' {redirectOrigin}; frame-ancestors 'none'; base-uri 'none'";
        var fields = Hidden(tokens.FormFieldName, tokens.RequestToken) +
            Hidden("id_token_hint", request.Hint) + Hidden("client_id", request.ClientId) +
            Hidden("post_logout_redirect_uri", request.RedirectUri) + Hidden("state", request.State);
        var message = invalidRedirect ? "The post-logout redirect URI is not registered. You can still sign out of this browser session." : "Sign out of this browser session?";
        var html = $"<!doctype html><html lang=\"en\"><title>Sign out</title><p>{message}</p><form method=\"post\" action=\"{WebUtility.HtmlEncode(action)}\">{fields}<button type=\"submit\">Sign out</button></form></html>";
        return Results.Content(html, "text/html; charset=utf-8", statusCode: invalidRedirect ? StatusCodes.Status400BadRequest : StatusCodes.Status200OK);
    }

    static string Hidden(string name, string? value) => value is null ? string.Empty : $"<input type=\"hidden\" name=\"{WebUtility.HtmlEncode(name)}\" value=\"{WebUtility.HtmlEncode(value)}\">";
}
