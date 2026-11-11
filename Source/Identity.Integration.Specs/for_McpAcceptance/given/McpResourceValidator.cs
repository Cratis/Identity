// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.for_McpAcceptance.given;

/// <summary>
/// An in-process RFC 9728 resource boundary with real signature, issuer, audience and lifetime validation.
/// This is not a product tool host or an AuthProxy revocation-cache implementation.
/// </summary>
public sealed class McpResourceValidator : IAsyncDisposable
{
    readonly WebApplication _application;
    readonly TokenValidationParameters _validation;
    readonly string _metadataUri;

    McpResourceValidator(WebApplication application, McpProduct product, Uri issuer)
    {
        _application = application;
        _metadataUri = new Uri(new Uri(product.Indicator), "/.well-known/oauth-protected-resource/mcp").AbsoluteUri;
        _validation = new TokenValidationParameters
        {
            ValidIssuer = issuer.AbsoluteUri,
            ValidAudience = product.Audience,
            ValidTypes = ["at+jwt"],
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };
        application.MapGet("/.well-known/oauth-protected-resource/mcp", () => Results.Json(new
        {
            resource = product.Indicator,
            authorization_servers = new[] { _validation.ValidIssuer },
            scopes_supported = new[] { product.Scope }
        }));
        application.MapGet("/mcp", (Delegate)Invoke);
    }

    public HttpClient Client { get; private set; }

    /// <summary>
    /// Starts an in-memory resource; no TCP listener or Docker resource is started here.
    /// </summary>
    /// <param name="product">The independently configured resource.</param>
    /// <param name="issuer">The trusted realm issuer.</param>
    /// <returns>The running resource boundary.</returns>
    public static async Task<McpResourceValidator> Start(McpProduct product, Uri issuer)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var application = builder.Build();
        var resource = new McpResourceValidator(application, product, issuer);
        await application.StartAsync();
        resource.Client = application.GetTestClient();
        resource.Client.BaseAddress = new Uri(product.Indicator);
        return resource;
    }

    /// <summary>
    /// Discovers Identity through the unauthenticated resource challenge and pins its published signing keys.
    /// </summary>
    /// <param name="browser">The Identity transport.</param>
    /// <param name="product">The expected RFC 8707 resource.</param>
    /// <returns>The discovered authorization and token endpoints.</returns>
    public async Task<(string AuthorizationEndpoint, string TokenEndpoint)> Discover(Browser browser, McpProduct product)
    {
        using var challenge = await Client.GetAsync("/mcp");
        challenge.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
        challenge.Headers.WwwAuthenticate.ToString().ShouldEqual($"Bearer resource_metadata=\"{_metadataUri}\"");
        var metadata = await Client.GetFromJsonAsync<JsonElement>(_metadataUri);
        metadata.GetProperty("resource").GetString().ShouldEqual(product.Indicator);
        var issuer = metadata.GetProperty("authorization_servers").EnumerateArray().Single().GetString();
        issuer.ShouldEqual(_validation.ValidIssuer);
        var discovery = await browser.Client.GetFromJsonAsync<JsonElement>($"{issuer!.TrimEnd('/')}/.well-known/oauth-authorization-server");
        discovery.GetProperty("issuer").GetString()!.TrimEnd('/').ShouldEqual(issuer.TrimEnd('/'));
        var jwks = await browser.Client.GetStringAsync(discovery.GetProperty("jwks_uri").GetString());
        _validation.IssuerSigningKeys = new JsonWebKeySet(jwks).GetSigningKeys();
        return (discovery.GetProperty("authorization_endpoint").GetString()!, discovery.GetProperty("token_endpoint").GetString()!);
    }

    public async Task<HttpStatusCode> Call(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await Client.SendAsync(request);
        return response.StatusCode;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _application.DisposeAsync();
    }

    async Task<IResult> Invoke(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(authorization, out var header) &&
            string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(header.Parameter))
        {
            var result = await new JsonWebTokenHandler().ValidateTokenAsync(header.Parameter, _validation);
            if (result.IsValid)
            {
                return Results.Ok();
            }
        }

        context.Response.Headers.WWWAuthenticate = $"Bearer resource_metadata=\"{_metadataUri}\"";
        return Results.Unauthorized();
    }
}
