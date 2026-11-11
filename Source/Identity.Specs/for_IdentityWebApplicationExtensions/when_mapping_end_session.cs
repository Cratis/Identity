// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_mapping_end_session : given.an_identity_host
{
    [Theory]
    [InlineData("")]
    [InlineData("/id")]
    public async Task should_register_get_and_post_with_the_same_protocol_prefix_and_keep_local_logout(string prefix)
    {
        var builder = CreateBuilder(prefix);
        builder.Configuration["Identity:Issuer"] = "https://identity.example/";
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        app.MapCratisIdentity(options => options.MapSignOut = true);
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Cast<RouteEndpoint>().ToArray();
        var endpoint = routes.Single(route => route.RoutePattern.RawText == $"{prefix}/connect/endsession");
        endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.ShouldContainOnly("GET", "POST");
        routes.Count(route => route.RoutePattern.RawText == $"{prefix}/logout").ShouldEqual(2);
        var server = app.Services.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
        server.EndSessionEndpointUris.Single().OriginalString.ShouldEqual($"{prefix}/connect/endsession".TrimStart('/'));
        server.Issuer!.AbsoluteUri.ShouldEqual("https://identity.example/");
    }

    [Fact]
    public async Task should_not_activate_end_session_without_an_explicit_issuer()
    {
        var builder = CreateBuilder();
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        app.MapCratisIdentity();
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Cast<RouteEndpoint>().ToArray();
        routes.Any(route => route.RoutePattern.RawText == "/connect/endsession").ShouldBeFalse();
        var server = app.Services.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
        server.EndSessionEndpointUris.ShouldBeEmpty();
        server.Handlers.Any(handler => handler.ContextType == typeof(ExtractEndSessionRequestContext) && handler.Order == int.MinValue + 100).ShouldBeFalse();
    }

    [Fact]
    public async Task should_skip_only_the_end_session_extraction_event()
    {
        var builder = CreateBuilder();
        builder.Configuration["Identity:Issuer"] = "https://identity.example/";
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        var server = app.Services.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
        var descriptor = server.Handlers.Single(handler => handler.ContextType == typeof(ExtractEndSessionRequestContext) && handler.Order == int.MinValue + 100);
        var handler = (IOpenIddictServerHandler<ExtractEndSessionRequestContext>)app.Services.GetRequiredService(descriptor.ServiceDescriptor.ServiceType);
        var context = new ExtractEndSessionRequestContext(new OpenIddictServerTransaction { Options = server, Logger = NullLogger.Instance, EndpointType = OpenIddictServerEndpointType.EndSession });
        await handler.HandleAsync(context);
        context.IsRequestSkipped.ShouldBeTrue();
        descriptor.ServiceDescriptor.ServiceType.GetGenericArguments().ShouldContainOnly(typeof(ExtractEndSessionRequestContext));
    }
}
