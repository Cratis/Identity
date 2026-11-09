// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRateLimitMiddleware;

public class when_mapped_before_authentication : Specification
{
    int _status;
    IAuthenticationService _authentication;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        var options = Options.Create(new IdentityOptions { TokenRateLimit = new() { PerSourceRequests = 1 } });
        var limiter = new TokenRequestRateLimiter(TimeProvider.System, options, Substitute.For<ILogger<TokenRequestRateLimiter>>());
        limiter.Acquire(null, null, "unknown");
        _authentication = Substitute.For<IAuthenticationService>();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(limiter);
        builder.Services.AddAuthentication();
        builder.Services.AddSingleton(_authentication);
        builder.Services.AddAntiforgery();
        builder.Services.AddTransient<AuthorizationEndpoint>();
        builder.Services.AddTransient<TokenEndpoint>();
        await using var app = builder.Build();
        app.MapCratisIdentity();
        var request = new DefaultHttpContext { RequestServices = app.Services };
        request.Request.Path = "/connect/token";
        request.Request.Method = "POST";
        request.Response.Body = new MemoryStream();
        await ((IApplicationBuilder)app).Build()(request);
        _status = request.Response.StatusCode;
    }

    [Fact] void should_limit_in_the_mapped_pipeline() => _status.ShouldEqual(429);
    [Fact] void should_not_authenticate() => _authentication.ReceivedCalls().ShouldBeEmpty();
}
