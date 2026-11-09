// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_TokenRateLimitMiddleware;

public class when_matching_openiddict_token_paths : Specification
{
    readonly List<int> _statuses = [];
    readonly List<OpenIddictServerEndpointType> _endpoints = [];

    async Task Because()
    {
        var options = Options.Create(new IdentityOptions { TokenRateLimit = new() { PerSourceRequests = 1 } });
        var limiter = new TokenRequestRateLimiter(TimeProvider.System, options, NullLogger<TokenRequestRateLimiter>.Instance);
        var middleware = new TokenRateLimitMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(Context("", "/connect/token"), limiter, options);
        (string Base, string Path)[] variants =
        [
            ("", "/connect/token/"),
            ("", "/CONNECT/ToKeN/"),
            ("/identity", "/connect/token/"),
            ("/identity/", "/CONNECT/TOKEN"),
            ("", "/connect/ignored/../token/"),
            ("/identity", "/connect/./token"),
            ("", "/connect/%74oken/"),
            ("", "/connect/ignored/%2e%2e/token")
        ];
        foreach (var variant in variants)
        {
            var context = Context(variant.Base, variant.Path);
            var transaction = new OpenIddictServerTransaction { Options = new(), Logger = NullLogger.Instance };
            transaction.Options.TokenEndpointUris.Add(new Uri("connect/token", UriKind.Relative));
            transaction.Properties[typeof(HttpRequest).FullName] = new WeakReference<HttpRequest>(context.Request);
            var notification = new ProcessRequestContext(transaction);
            await new OpenIddictServerAspNetCoreHandlers.ResolveRequestUri().HandleAsync(notification);
            await new OpenIddictServerHandlers.InferEndpointType().HandleAsync(notification);
            _endpoints.Add(notification.EndpointType);
            await middleware.InvokeAsync(context, limiter, options);
            _statuses.Add(context.Response.StatusCode);
        }
    }

    static DefaultHttpContext Context(string pathBase, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("identity.example");
        context.Request.PathBase = pathBase;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString("?ignored=1");
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact] void should_exercise_paths_recognized_by_the_pinned_openiddict() => _endpoints.TrueForAll(_ => _ == OpenIddictServerEndpointType.Token).ShouldBeTrue();
    [Fact] void should_refuse_every_openiddict_token_path_after_the_source_budget() => _statuses.TrueForAll(_ => _ == 429).ShouldBeTrue();
}
