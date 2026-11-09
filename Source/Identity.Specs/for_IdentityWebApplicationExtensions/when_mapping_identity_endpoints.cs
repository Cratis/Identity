// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Realm;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_mapping_identity_endpoints : given.an_identity_host
{
    [Fact]
    async Task should_map_only_authorization_and_token_without_adding_middleware()
    {
        var builder = CreateBuilder();
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        app.MapCratisIdentity();
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Cast<RouteEndpoint>().ToArray();
        routes.Select(route => route.RoutePattern.RawText).ShouldContainOnly("/connect/authorize", "/connect/token");
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
