// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Hosting;
using Cratis.Identity.Realm;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_mapping_identity_endpoints : given.an_identity_host
{
    [Fact]
    async Task should_map_authorization_token_end_session_and_provider_discovery_without_adding_middleware()
    {
        var builder = CreateBuilder();
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        app.MapCratisIdentity();
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Cast<RouteEndpoint>().ToArray();
        routes.Select(route => route.RoutePattern.RawText).ShouldContainOnly("/connect/authorize", "/connect/token", "/connect/endsession", "/.well-known/openid-configuration");
        routes.Single(route => route.RoutePattern.RawText == "/connect/token").Metadata.GetMetadata<HttpMethodMetadata>().HttpMethods.ShouldContainOnly("POST");
        var invoked = false;
        app.Run(context =>
        {
            invoked = true;
            IdentityRealmScope.IsActive.ShouldBeFalse();
            return Task.CompletedTask;
        });
        var response = await Request(app, "/host-work");
        invoked.ShouldBeTrue();
        response.Response.Headers.ContainsKey("X-Frame-Options").ShouldBeFalse();
        response.Response.Headers.ContainsKey("Referrer-Policy").ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("/id")]
    async Task should_map_device_routes_and_protocol_uris_with_the_prefix(string prefix)
    {
        var builder = CreateBuilder(prefix);
        builder.Configuration["Identity:Device:Enabled"] = "true";
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        app.MapCratisIdentity();
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Cast<RouteEndpoint>().ToArray();
        routes.Select(route => route.RoutePattern.RawText).ShouldContainOnly($"{prefix}/connect/authorize", $"{prefix}/connect/token", $"{prefix}/connect/endsession", $"{prefix}/connect/device", $"{prefix}/device", "/.well-known/openid-configuration");
        var server = app.Services.GetRequiredService<IOptions<OpenIddict.Server.OpenIddictServerOptions>>().Value;
        server.DeviceAuthorizationEndpointUris.Single().OriginalString.ShouldEqual($"{prefix}/connect/device".TrimStart('/'));
        server.EndUserVerificationEndpointUris.Single().OriginalString.ShouldEqual($"{prefix}/device".TrimStart('/'));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    async Task should_reject_repeat_mapping_without_changing_sign_out(bool first, bool second)
    {
        var builder = CreateBuilder();
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        app.MapCratisIdentity(options => options.MapSignOut = first);
        var count = ((IEndpointRouteBuilder)app).DataSources.Sum(source => source.Endpoints.Count);
        var exception = Catch.Exception(() => app.MapCratisIdentity(options => options.MapSignOut = second));
        exception.ShouldBeOfExactType<InvalidOperationException>();
        exception.Message.ShouldContain("once per host");
        app.Services.GetRequiredService<IOptions<IdentityEndpointOptions>>().Value.MapSignOut.ShouldEqual(first);
        ((IEndpointRouteBuilder)app).DataSources.Sum(source => source.Endpoints.Count).ShouldEqual(count);
    }

    [Theory]
    [InlineData("", "/connect/authorize", "GET")]
    [InlineData("", "/connect/token", "POST")]
    [InlineData("/id", "/id/connect/authorize", "GET")]
    [InlineData("/id", "/id/connect/token", "POST")]
    async Task should_fail_closed_without_identity_middleware(string prefix, string path, string method)
    {
        var builder = CreateBuilder(prefix);
        builder.AddCratisIdentity();

        // No business dependencies: a missing validated OpenIddict request must be refused before any are used.
        builder.Services.AddSingleton(new AuthorizationEndpoint(null!, null!, null!, null!, null!, null!));
        builder.Services.AddSingleton(new TokenEndpoint(null!, null!, null!, null!, null!, null!, null!, null!));
        await using var app = builder.Build();
        app.UseRouting();
        app.MapCratisIdentity();
        app.Run(context => context.GetEndpoint().RequestDelegate(context));
        var exception = await Catch.Exception(async () => await Request(app, path, method));
        exception.ShouldBeOfExactType<OpenIddictRequestMissing>();
        exception.Message.ShouldContain(path);
    }

    [Theory]
    [InlineData("/logout", "GET")]
    [InlineData("/logout", "POST")]
    [InlineData("/", "GET")]
    [InlineData("/healthz", "GET")]
    async Task should_leave_host_and_sign_out_routes_unmapped(string path, string method)
    {
        var builder = CreateBuilder();
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        app.UseRouting();
        app.MapCratisIdentity();
        app.Run(context =>
        {
            context.GetEndpoint().ShouldBeNull();
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        var response = await Request(app, path, method);
        response.Response.StatusCode.ShouldEqual(StatusCodes.Status404NotFound);
    }
}
