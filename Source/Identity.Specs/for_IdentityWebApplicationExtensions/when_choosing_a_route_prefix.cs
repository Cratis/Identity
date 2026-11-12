// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Authorization;
using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_choosing_a_route_prefix : given.an_identity_host
{
    [Theory]
    [InlineData("")]
    [InlineData("/id")]
    [InlineData("/identity/v1")]
    [InlineData("/id-._~1")]
    async Task should_expose_the_validated_prefix_for_middleware_and_openiddict(string prefix)
    {
        var builder = CreateBuilder(prefix);
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        app.UseCratisIdentity();
        app.MapCratisIdentity();
        app.Services.GetRequiredService<IOptions<IdentityEndpointOptions>>().Value.RoutePrefix.Value.ShouldEqual(prefix);
        var server = app.Services.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue;
        server.AuthorizationEndpointUris.Single().OriginalString.ShouldEqual($"{prefix}/connect/authorize".TrimStart('/'));
        server.TokenEndpointUris.Single().OriginalString.ShouldEqual($"{prefix}/connect/token".TrimStart('/'));
        server.RevocationEndpointUris.Single().OriginalString.ShouldEqual($"{prefix}/connect/revocation".TrimStart('/'));
        server.IntrospectionEndpointUris.Single().OriginalString.ShouldEqual($"{prefix}/connect/introspect".TrimStart('/'));
        server.Issuer.AbsoluteUri.ShouldEqual("https://identity.example/");
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Cast<RouteEndpoint>()
            .Select(route => route.RoutePattern.RawText).ShouldContainOnly($"{prefix}/connect/authorize", $"{prefix}/connect/token", $"{prefix}/connect/endsession", "/.well-known/openid-configuration");
    }

    [Theory]
    [InlineData("/id/connect/token", "POST", 500)]
    [InlineData("/id/connect/authorize", "GET", 500)]
    [InlineData("/connect/token", "POST", 404)]
    [InlineData("/connect/authorize", "GET", 404)]
    [InlineData("/.well-known/openid-configuration", "GET", 200)]
    [InlineData("/.well-known/oauth-authorization-server", "GET", 200)]
    [InlineData("/id/.well-known/openid-configuration", "GET", 404)]
    async Task should_dispatch_prefixed_protocol_requests_and_leave_discovery_unprefixed(string path, string method, int status)
    {
        var builder = CreateBuilder("/id");
        builder.AddCratisIdentity();
        builder.Services.PostConfigure<OAuthOptions>(IdentitySchemes.GitHub, options => options.EventsType = null);
        await using var app = builder.Build();
        app.UseRouting();
        app.UseCratisIdentity();
        app.MapCratisIdentity();
        app.Run(context =>
        {
            if (context.GetEndpoint() is { } endpoint) return endpoint.RequestDelegate(context);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        var response = await Request(app, path, method);
        if (status == StatusCodes.Status500InternalServerError)
        {
            response.Response.Body.Position = 0;
            using var reader = new StreamReader(response.Response.Body);
            (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldContain("The server is recovering its restriction state.");
        }

        response.Response.StatusCode.ShouldEqual(status);
        if (status == StatusCodes.Status200OK)
        {
            response.Response.Body.Position = 0;
            using var document = await JsonDocument.ParseAsync(response.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
            document.RootElement.GetProperty("issuer").GetString().ShouldEqual("https://identity.example/");
            document.RootElement.GetProperty("jwks_uri").GetString().ShouldNotContain("/id/");
            if (path == "/.well-known/openid-configuration")
            {
                document.RootElement.TryGetProperty("token_endpoint", out _).ShouldBeFalse();
                document.RootElement.TryGetProperty("authorization_endpoint", out _).ShouldBeFalse();
            }
            else
            {
                document.RootElement.GetProperty("token_endpoint").GetString().ShouldEqual("https://identity.example/id/connect/token");
                document.RootElement.GetProperty("authorization_endpoint").GetString().ShouldEqual("https://identity.example/id/connect/authorize");
            }
        }
    }
}
