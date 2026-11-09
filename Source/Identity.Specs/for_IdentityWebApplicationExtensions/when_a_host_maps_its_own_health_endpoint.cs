// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Realm;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_a_host_maps_its_own_health_endpoint : given.an_identity_host
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    async Task should_serve_host_routes_without_collisions(bool useIdentity, bool mapHostFirst)
    {
        var builder = CreateBuilder();
        builder.AddCratisIdentity();
        builder.Services.AddSingleton(Substitute.For<IAuthenticationSchemeProvider>());
        await using var app = builder.Build();
        if (useIdentity)
        {
            app.UseCratisIdentity();
        }
        else
        {
            app.UseRouting();
        }

        if (mapHostFirst)
        {
            MapHost(app);
        }

        app.MapCratisIdentity();
        if (!mapHostFirst)
        {
            MapHost(app);
        }

        app.Run(context => context.GetEndpoint().RequestDelegate(context));
        foreach (var path in new[] { "/healthz", "/" })
        {
            var response = await Request(app, path);
            response.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
            response.Response.Body.Position = 0;
            using var reader = new StreamReader(response.Response.Body);
            (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldEqual("host-ok");
            response.Response.Headers.ContainsKey("X-Frame-Options").ShouldEqual(useIdentity);
            response.Response.Headers.ContainsKey("Referrer-Policy").ShouldEqual(useIdentity);
        }
    }

    static void MapHost(WebApplication app)
    {
        app.MapGet("/healthz", () =>
        {
            IdentityRealmScope.IsActive.ShouldBeFalse();
            return Results.Text("host-ok");
        });
        app.MapGet("/", () => Results.Text("host-ok"));
    }
}
