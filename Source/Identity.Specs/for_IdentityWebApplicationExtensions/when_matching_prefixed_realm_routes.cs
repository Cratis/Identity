// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Realm;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_matching_prefixed_realm_routes : given.an_identity_host
{
    [Theory]
    [InlineData("/id/connect/token", false, true)]
    [InlineData("/id/connect/authorize", false, true)]
    [InlineData("/id/connect/introspect", false, true)]
    [InlineData("/id/connect/revocation", false, true)]
    [InlineData("/connect/token", false, false)]
    [InlineData("/identity/connect/token", false, false)]
    [InlineData("/id/connectevil", false, false)]
    [InlineData("/.well-known/jwks", false, true)]
    [InlineData("/signin-github", false, true)]
    [InlineData("/id/logout", false, false)]
    [InlineData("/id/logout", true, true)]
    [InlineData("/logout", true, false)]
    [InlineData("/id/logoutevil", true, false)]
    [InlineData("/id/connect/../host-work", false, false)]
    [InlineData("/id/connect/%2e%2e/host-work", false, false)]
    [InlineData("/id/connect//token", false, false)]
    [InlineData("/id/api/cratis/identity/accounts/mine", false, true)]
    [InlineData("/id/api/cratis/identity/accountsevil", false, false)]
    [InlineData("/api/cratis/identity/accounts/mine", false, false)]
    [InlineData("/healthz", false, false)]
    async Task should_scope_before_authentication_only_for_matching_routes(string path, bool signOut, bool expected)
    {
        var builder = CreateBuilder("/id");
        builder.AddCratisIdentity();
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        schemes.GetRequestHandlerSchemesAsync().Returns([new AuthenticationScheme("probe", "probe", typeof(OAuthHandler<OAuthOptions>))]);
        builder.Services.AddSingleton(schemes);
        var handlers = Substitute.For<IAuthenticationHandlerProvider>();
        var handler = Substitute.For<IAuthenticationRequestHandler>();
        var duringAuthentication = false;
        handler.HandleRequestAsync().Returns(_ =>
        {
            duringAuthentication = IdentityRealmScope.IsActive;
            return false;
        });
        handlers.GetHandlerAsync(Arg.Any<Microsoft.AspNetCore.Http.HttpContext>(), "probe").Returns(handler);
        builder.Services.AddSingleton(handlers);
        await using var app = builder.Build();
        app.UseCratisIdentity();
        app.MapCratisIdentity(options => options.MapSignOut = signOut);
        var downstream = false;
        app.Run(_ =>
        {
            downstream = IdentityRealmScope.IsActive;
            return Task.CompletedTask;
        });
        await Request(app, path);
        duringAuthentication.ShouldEqual(expected);
        downstream.ShouldEqual(expected);
        IdentityRealmScope.IsActive.ShouldBeFalse();
    }
}
