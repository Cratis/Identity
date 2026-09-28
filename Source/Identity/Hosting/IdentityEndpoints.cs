// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.Hosting;

/// <summary>
/// Extension methods for mapping the Identity middleware and endpoints.
/// </summary>
public static class IdentityEndpoints
{
    /// <summary>
    /// Adds the Identity middleware and maps its endpoints. The protocol endpoints themselves (discovery, JWKS,
    /// revocation, introspection) are served by OpenIddict; authorization and token requests pass through to
    /// <see cref="AuthorizationEndpoint"/> and <see cref="TokenEndpoint"/> once OpenIddict has validated them.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/>.</param>
    /// <returns>The <see cref="WebApplication"/> for continuation.</returns>
    public static WebApplication UseCratisIdentity(this WebApplication app)
    {
        app.UseSecurityHeaders();
        app.UseAuthentication();

        app.MapMethods("/connect/authorize", [HttpMethods.Get, HttpMethods.Post], (HttpContext context, AuthorizationEndpoint endpoint) => endpoint.Handle(context))
            .DisableAntiforgery();
        app.MapPost("/connect/token", (HttpContext context, TokenEndpoint endpoint) => endpoint.Handle(context))
            .DisableAntiforgery();
        app.MapGet("/healthz", () => Results.Text("ok"));
        app.MapGet("/", () => Results.Text("Cratis Identity - OAuth 2.1 authorization server. Metadata: /.well-known/oauth-authorization-server"));

        return app;
    }
}
