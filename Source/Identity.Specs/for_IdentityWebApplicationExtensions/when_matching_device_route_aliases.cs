// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Realm;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_matching_device_route_aliases : given.an_identity_host
{
    [Theory]
    [InlineData("", "/device", true, true)]
    [InlineData("", "/device/", true, true)]
    [InlineData("/id", "/id/device", true, true)]
    [InlineData("/id", "/id/device/", true, true)]
    [InlineData("/id", "/ID/DEVICE/", true, true)]
    [InlineData("", "/device", false, false)]
    [InlineData("", "/device/", false, false)]
    [InlineData("/id", "/id/device", false, false)]
    [InlineData("/id", "/id/device/", false, false)]
    [InlineData("/id", "/device/", true, false)]
    [InlineData("/id", "/id/deviceevil", true, false)]
    [InlineData("/id", "/id/device/extra", true, false)]
    async Task should_scope_and_dispatch_the_same_verification_aliases(string prefix, string path, bool enabled, bool expected)
    {
        var builder = CreateBuilder(prefix);
        builder.Configuration["Identity:Device:Enabled"] = enabled.ToString();
        builder.AddCratisIdentity();
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        schemes.GetRequestHandlerSchemesAsync().Returns([]);
        builder.Services.AddSingleton(schemes);
        await using var app = builder.Build();
        app.UseRouting();
        app.UseCratisIdentity();
        app.MapCratisIdentity();
        var scoped = false;
        var dispatched = false;
        app.Use((HttpContext context, RequestDelegate next) =>
        {
            // Observe routing without invoking the external sign-in challenge.
            scoped = IdentityRealmScope.IsActive;
            dispatched = context.GetEndpoint() is not null;
            return Task.CompletedTask;
        });
        await Request(app, path);
        scoped.ShouldEqual(expected);
        dispatched.ShouldEqual(expected);
        IdentityRealmScope.IsActive.ShouldBeFalse();
    }
}
