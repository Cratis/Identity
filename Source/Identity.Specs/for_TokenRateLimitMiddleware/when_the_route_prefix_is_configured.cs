// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRateLimitMiddleware;

public class when_the_route_prefix_is_configured : Specification
{
    int _prefixedStatus;
    int _unprefixedStatus;
    int _otherEndpointStatus;

    async Task Because()
    {
        var options = Options.Create(new IdentityOptions { TokenRateLimit = new() { PerSourceRequests = 1 } });
        var endpoints = Options.Create(new IdentityEndpointOptions { RoutePrefix = "/realm" });
        var limiter = new TokenRequestRateLimiter(TimeProvider.System, options, NullLogger<TokenRequestRateLimiter>.Instance);
        var middleware = new TokenRateLimitMiddleware(_ => Task.CompletedTask, endpoints);

        // The first prefixed request spends the whole budget for the source.
        await middleware.InvokeAsync(Context("/realm/connect/token"), limiter, options);
        var prefixed = Context("/realm/connect/token");
        await middleware.InvokeAsync(prefixed, limiter, options);
        _prefixedStatus = prefixed.Response.StatusCode;

        var unprefixed = Context("/connect/token");
        await middleware.InvokeAsync(unprefixed, limiter, options);
        _unprefixedStatus = unprefixed.Response.StatusCode;

        var other = Context("/realm/connect/authorize");
        await middleware.InvokeAsync(other, limiter, options);
        _otherEndpointStatus = other.Response.StatusCode;
    }

    static DefaultHttpContext Context(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("identity.example");
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact] void should_limit_the_prefixed_token_endpoint() => _prefixedStatus.ShouldEqual(429);
    [Fact] void should_not_limit_the_unprefixed_path_that_openiddict_does_not_serve() => _unprefixedStatus.ShouldEqual(200);
    [Fact] void should_not_limit_other_prefixed_endpoints() => _otherEndpointStatus.ShouldEqual(200);
}
