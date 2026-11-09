// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRateLimitMiddleware;

public class when_forwarded_sources_are_untrusted : Specification
{
    int _untrustedStatus;
    int _trustedStatus;

    async Task Because()
    {
        var options = Options.Create(new IdentityOptions { TokenRateLimit = new() { PerSourceRequests = 1 } });
        var limiter = new TokenRequestRateLimiter(TimeProvider.System, options, NullLogger<TokenRequestRateLimiter>.Instance);
        var admission = new TokenRateLimitMiddleware(_ => Task.CompletedTask);
        var forwarding = Options.Create(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor });
        var middleware = new ForwardedHeadersMiddleware(context => admission.InvokeAsync(context, limiter, options), NullLoggerFactory.Instance, forwarding);
        await middleware.Invoke(Context("192.0.2.1", "198.51.100.1"));
        var forged = Context("192.0.2.1", "198.51.100.2");
        forged.Request.Headers["Forwarded"] = "for=198.51.100.3";
        forged.Request.Headers["X-Real-IP"] = "198.51.100.4";
        await middleware.Invoke(forged);
        _untrustedStatus = forged.Response.StatusCode;
        await middleware.Invoke(Context("127.0.0.1", "198.51.100.1"));
        var trusted = Context("127.0.0.1", "198.51.100.2");
        await middleware.Invoke(trusted);
        _trustedStatus = trusted.Response.StatusCode;
    }

    static DefaultHttpContext Context(string remote, string forwarded)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        context.Request.Path = "/connect/token";
        context.Request.Headers["X-Forwarded-For"] = forwarded;
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact] void should_not_allow_rotating_untrusted_headers_to_bypass_the_source_budget() => _untrustedStatus.ShouldEqual(429);
    [Fact] void should_use_the_source_resolved_by_host_configured_trust() => _trustedStatus.ShouldEqual(200);
}
