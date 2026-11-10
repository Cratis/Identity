// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// A minimal protected resource, not an MCP implementation. Its canonical resource is the registered Direct
/// indicator; loopback HTTP is only the test transport, not an additional registered resource or issuer.
/// </summary>
public sealed class McpResourceStub : IAsyncDisposable
{
    readonly WebApplication _app;
    TokenValidationParameters _validation = new();

    McpResourceStub(WebApplication app)
    {
        _app = app;
        app.MapGet("/.well-known/oauth-protected-resource/mcp", () => Results.Json(new
        {
            resource = IdentityHost.DirectMcp,
            authorization_servers = new[] { IdentityHost.Issuer },
            scopes_supported = new[] { "direct:mcp:read" }
        }));
        app.MapGet("/mcp", (Delegate)Protect);
    }

    public Uri Address => new(_app.Urls.Single());

    /// <summary>
    /// Starts Kestrel on an ephemeral loopback port.
    /// </summary>
    /// <returns>The running resource.</returns>
    public static async Task<McpResourceStub> Start()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var resource = new McpResourceStub(builder.Build());
        await resource._app.StartAsync();
        return resource;
    }

    /// <summary>
    /// Configures resource validation from the discovered issuer and its published keys.
    /// </summary>
    /// <param name="issuer">The discovered issuer.</param>
    /// <param name="keys">The published signing keys.</param>
    /// <param name="audience">The audience the resource accepts; the Direct MCP audience when omitted.</param>
    public void Trust(string issuer, IEnumerable<SecurityKey> keys, string? audience = null) => _validation = new TokenValidationParameters
    {
        ValidIssuer = issuer,
        ValidAudience = audience ?? IdentityHost.DirectMcpAudience,
        IssuerSigningKeys = keys,
        ValidTypes = ["at+jwt"]
    };

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    async Task<IResult> Protect(HttpContext context)
    {
        if (AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var header) &&
            header.Scheme == "Bearer" && header.Parameter is { } token)
        {
            var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token, _validation);
            if (validation.IsValid)
            {
                return Results.Ok();
            }
        }

        context.Response.Headers.WWWAuthenticate = $"Bearer resource_metadata=\"{new Uri(Address, "/.well-known/oauth-protected-resource/mcp")}\"";
        return Results.Unauthorized();
    }
}
