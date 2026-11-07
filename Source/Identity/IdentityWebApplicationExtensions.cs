// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Authorization;
using Cratis.Identity.Device;
using Cratis.Identity.Hosting;
using Cratis.Identity.Realm;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding the Identity middleware and mapping its endpoints.
/// </summary>
public static class IdentityWebApplicationExtensions
{
    /// <summary>
    /// Adds routing, the Identity realm scope, security headers and authentication middleware, then maps the logout, health and root endpoints. Call it
    /// before the host's own endpoints. The protocol endpoints themselves (discovery, JWKS,
    /// revocation, introspection) are served by OpenIddict; authorization and token requests pass through to
    /// <see cref="AuthorizationEndpoint"/> and <see cref="TokenEndpoint"/> once OpenIddict has validated them.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/>.</param>
    /// <returns>The <see cref="WebApplication"/> for continuation.</returns>
    public static WebApplication MapCratisIdentity(this WebApplication app)
    {
        app.UseRouting();
        app.UseMiddleware<IdentityRealmMiddleware>();
        app.UseSecurityHeaders();
        app.UseAuthentication();

        if (app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Cratis.Identity.Configuration.IdentityOptions>>().Value.DeviceGrant.Enabled)
        {
            DeviceAuthorizationEndpoint.Map(app);
            DeviceVerificationEndpoint.Map(app);
        }

        app.MapMethods("/connect/authorize", [HttpMethods.Get, HttpMethods.Post], (HttpContext context, AuthorizationEndpoint endpoint) => endpoint.Handle(context))
            .DisableAntiforgery();
        app.MapPost("/connect/token", (HttpContext context, TokenEndpoint endpoint) => endpoint.Handle(context))
            .DisableAntiforgery();
        app.MapGet("/logout", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var token = antiforgery.GetAndStoreTokens(context);
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
            return Results.Content($"<!doctype html><html lang=\"en\"><title>Sign out</title><form method=\"post\" action=\"/logout\"><input type=\"hidden\" name=\"{WebUtility.HtmlEncode(token.FormFieldName)}\" value=\"{WebUtility.HtmlEncode(token.RequestToken)}\"><button type=\"submit\">Sign out</button></form></html>", "text/html; charset=utf-8");
        });
        app.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
        {
            if (!await antiforgery.IsRequestValidAsync(context))
            {
                return Results.BadRequest("The sign-out form expired or did not come from this browser session.");
            }

            await context.SignOutAsync(IdentitySchemes.Session);
            context.Response.Headers.CacheControl = "no-store";
            return Results.Text("Signed out.");
        }).DisableAntiforgery();
        app.MapGet("/healthz", () => Results.Text("ok"));
        app.MapGet("/", () => Results.Text("Cratis Identity - OAuth 2.1 authorization server. Metadata: /.well-known/oauth-authorization-server"));

        return app;
    }
}
