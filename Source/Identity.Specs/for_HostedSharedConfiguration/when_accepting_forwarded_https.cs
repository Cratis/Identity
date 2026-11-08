// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_accepting_forwarded_https : Specification
{
    [Theory]
    [InlineData("192.0.2.10", true)]
    [InlineData("198.51.100.10", false)]
    public async Task should_only_accept_https_from_a_trusted_proxy(string remoteAddress, bool expectedHttps)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ForwardedHeaders:KnownProxies:0"] = "192.0.2.10" });
        HostedSharedHosting.AddProxyHeaders(builder);
        await using var app = builder.Build();
        app.UseForwardedHeaders();
        var https = false;
        app.Run(context =>
        {
            https = context.Request.IsHttps;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Scheme = "http";
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.5";
        await ((IApplicationBuilder)app).Build()(context);
        https.ShouldEqual(expectedHttps);
    }
}
