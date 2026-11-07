// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Authorization;
using Cratis.Identity.Realm;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_authentication_resolves_namespace_bound_services : Specification
{
    [Theory]
    [InlineData("/signin-github", true, false)]
    [InlineData("/connect/token", true, true)]
    [InlineData("/.well-known/openid-configuration", true, true)]
    [InlineData("/logout", true, true)]
    [InlineData("/api/cratis/identity/accounts/mine", true, true)]
    [InlineData("/host-work", false, true)]
    public async Task should_bind_during_authentication_to_the_route_namespace(string path, bool identity, bool reachesNext)
    {
        EventStoreNamespaceName? authenticationNamespace = null;
        EventStoreNamespaceName? downstreamNamespace = null;
        var routed = false;
        var tenantResolver = Substitute.For<IEventStoreNamespaceResolver>();
        tenantResolver.Resolve().Returns(new EventStoreNamespaceName("tenant-alpha"));
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IEventStoreNamespaceResolver>(new IdentityRealmNamespaceResolver(tenantResolver));
        builder.Services.AddTransient<AuthorizationEndpoint>();
        builder.Services.AddTransient<TokenEndpoint>();
        builder.Services.AddAntiforgery();
        builder.Services.AddScoped(provider => new NamespaceBinding(provider.GetRequiredService<IEventStoreNamespaceResolver>().Resolve()));
        builder.Services.AddScoped(provider =>
        {
            authenticationNamespace = provider.GetRequiredService<NamespaceBinding>().Namespace;
            return new OAuthEvents
            {
                OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
        builder.Services.AddAuthentication("session").AddCookie("session").AddOAuth("upstream", options =>
        {
            options.ClientId = "spec-client";
            options.ClientSecret = "spec-secret";
            options.CallbackPath = "/signin-github";
            options.AuthorizationEndpoint = "https://upstream.example/authorize";
            options.TokenEndpoint = "https://upstream.example/token";
            options.SignInScheme = "session";
            options.EventsType = typeof(OAuthEvents);
        });
        await using var app = builder.Build();
        app.MapCratisIdentity();
        app.MapGet("/api/cratis/identity/accounts/mine", () => Results.NoContent());
        app.MapGet("/host-work", () => Results.NoContent());
        app.Run(context =>
        {
            downstreamNamespace = context.RequestServices.GetRequiredService<NamespaceBinding>().Namespace;
            routed = context.GetEndpoint() is not null;
            return Task.CompletedTask;
        });
        await using var scope = app.Services.CreateAsyncScope();
        var request = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        request.Request.Path = path;

        await ((IApplicationBuilder)app).Build()(request);

        var expected = identity ? EventStoreNamespaceName.Default : new EventStoreNamespaceName("tenant-alpha");
        authenticationNamespace.ShouldEqual(expected);
        downstreamNamespace.ShouldEqual(reachesNext ? expected : null);
        if (path.StartsWith("/api/", StringComparison.Ordinal) || string.Equals(path, "/host-work", StringComparison.Ordinal))
        {
            routed.ShouldBeTrue();
        }
        IdentityRealmScope.IsActive.ShouldBeFalse();
    }

    sealed record NamespaceBinding(EventStoreNamespaceName Namespace);
}
