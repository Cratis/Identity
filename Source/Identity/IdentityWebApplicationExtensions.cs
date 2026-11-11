// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Authorization;
using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Device;
using Cratis.Identity.Hosting;
using Cratis.Identity.Realm;
using Cratis.Identity.Sessions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding the Identity middleware and mapping its endpoints.
/// </summary>
public static class IdentityWebApplicationExtensions
{
    /// <summary>
    /// Adds routing, the Identity realm scope, security headers and authentication middleware.
    /// Call before Arc/Chronicle middleware and endpoint execution. This does not map endpoints.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/>.</param>
    /// <returns>The <see cref="WebApplication"/> for continuation.</returns>
    public static WebApplication UseCratisIdentity(this WebApplication app)
    {
        app.UseRouting();
        app.UseMiddleware<TokenRateLimitMiddleware>();
        app.UseMiddleware<IdentityRealmMiddleware>();
        app.UseSecurityHeaders();
        app.UseAuthentication();
        return app;
    }

    /// <summary>
    /// Maps authorization, token and explicitly configured end-session endpoints, and optionally the sign-out form. No middleware, health or root routes are added.
    /// OpenIddict serves discovery, JWKS, revocation and introspection through authentication middleware.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for continuation.</returns>
    public static IEndpointRouteBuilder MapCratisIdentity(this IEndpointRouteBuilder app) => app.MapCratisIdentity(null);

    /// <summary>
    /// Maps Identity endpoints with explicit mapping settings. No middleware, health or root routes are added.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="configure">Endpoint mapping settings. The prefix must match Identity:Endpoints:RoutePrefix.</param>
    /// <returns>The endpoint route builder for continuation.</returns>
    public static IEndpointRouteBuilder MapCratisIdentity(this IEndpointRouteBuilder app, Action<IdentityEndpointOptions>? configure)
    {
        var options = app.ServiceProvider.ResolveIdentityEndpoints(configure);
        var group = app.MapGroup(options.RoutePrefix.Value ?? string.Empty);
        if (app.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Cratis.Identity.Configuration.DeviceGrantOptions>>().Value.Enabled)
        {
            DeviceAuthorizationEndpoint.Map(group);
            DeviceVerificationEndpoint.Map(group);
        }

        group.MapMethods("/connect/authorize", [HttpMethods.Get, HttpMethods.Post], (HttpContext context, AuthorizationEndpoint endpoint) => endpoint.Handle(context))
            .DisableAntiforgery();
        group.MapPost("/connect/token", (HttpContext context, TokenEndpoint endpoint) => endpoint.Handle(context))
            .DisableAntiforgery();
        if (options.EndSessionEnabled)
        {
            group.MapMethods("/connect/endsession", [HttpMethods.Get, HttpMethods.Post], (HttpContext context, EndSessionEndpoint endpoint) => endpoint.Handle(context))
                .DisableAntiforgery();
        }

        if (options.MapSignOut)
        {
            MapSignOut(app, $"{options.RoutePrefix}/logout");
        }

        return app;
    }

    static void MapSignOut(IEndpointRouteBuilder app, string path)
    {
        app.MapGet(path, (HttpContext context, IAntiforgery antiforgery) =>
        {
            var token = antiforgery.GetAndStoreTokens(context);
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
            return Results.Content($"<!doctype html><html lang=\"en\"><title>Sign out</title><form method=\"post\" action=\"{WebUtility.HtmlEncode(path)}\"><input type=\"hidden\" name=\"{WebUtility.HtmlEncode(token.FormFieldName)}\" value=\"{WebUtility.HtmlEncode(token.RequestToken)}\"><button type=\"submit\">Sign out</button></form></html>", "text/html; charset=utf-8");
        });
        app.MapPost(path, async (HttpContext context, IAntiforgery antiforgery) =>
        {
            if (!await antiforgery.IsRequestValidAsync(context))
            {
                return Results.BadRequest("The sign-out form expired or did not come from this browser session.");
            }

            await context.SignOutAsync(IdentitySchemes.Session);
            context.Response.Headers.CacheControl = "no-store";
            return Results.Text("Signed out.");
        }).DisableAntiforgery();
    }
}
