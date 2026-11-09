// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace Cratis.Identity.for_TokenRateLimitMiddleware;

public class when_mapped_before_authentication : given.an_identity_host
{
    readonly List<HttpStatusCode> _admitted = [];
    readonly List<HttpStatusCode> _limited = [];
    readonly List<string> _admittedBodies = [];
    readonly List<string> _errors = [];
    int _authenticationRequests;
    int _admittedAuthenticationRequests;
    TokenRequestRateLimiter _limiter;
    IdentityOptions _options;
    OpenIddictServerOptions _serverOptions;

    void Establish()
    {
        RegisterIdentity();

        // ProcessRequest is dispatched by OpenIddict's real authentication request handler.
        // Observe its entry without replacing authentication or handling the request.
        Builder.Services.AddOpenIddict().AddServer(options => options.AddEventHandler<OpenIddictServerEvents.ProcessRequestContext>(handler => handler
            .SetOrder(int.MinValue)
            .UseInlineHandler(_ =>
            {
                _authenticationRequests++;
                return default;
            })));
        App = Builder.Build();
        App.MapCratisIdentity();
    }

    async Task Because()
    {
        await App.StartAsync(TestContext.Current.CancellationToken);
        _limiter = App.Services.GetRequiredService<TokenRequestRateLimiter>();
        _options = App.Services.GetRequiredService<IOptions<IdentityOptions>>().Value;
        _serverOptions = App.Services.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
        using var client = App.GetTestClient();

        // Without external recovery workers, the real OpenIddict recovery handler must fail closed.
        foreach (var path in new[] { "/connect/token", "/connect/token/" })
        {
            using var response = await client.PostAsync(path, new FormUrlEncodedContent([]), TestContext.Current.CancellationToken);
            _admitted.Add(response.StatusCode);
            _admittedBodies.Add(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        _admittedAuthenticationRequests = _authenticationRequests;
        foreach (var path in new[] { "/connect/token", "/connect/token/" })
        {
            using var response = await client.PostAsync(path, new FormUrlEncodedContent([]), TestContext.Current.CancellationToken);
            _limited.Add(response.StatusCode);
            _errors.Add(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
    }

    [Fact] void should_resolve_the_registered_limiter() => _limiter.ShouldNotBeNull();
    [Fact] void should_enable_rate_limiting_by_default() => _options.TokenRateLimit.Enabled.ShouldBeTrue();
    [Fact] void should_bind_the_source_budget() => _options.TokenRateLimit.PerSourceRequests.ShouldEqual(2);
    [Fact] void should_leave_token_exchange_unconfigured() => _options.TokenExchange.Clients.ShouldBeEmpty();
    [Fact] void should_validate_the_real_openiddict_options() => _serverOptions.TokenEndpointUris.ShouldNotBeEmpty();
    [Fact] void should_reach_openiddict_for_both_paths_within_budget() => _admittedAuthenticationRequests.ShouldEqual(2);

    [Fact] void should_reach_the_openiddict_recovery_handler() => _admittedBodies.TrueForAll(body => body.Contains("The server is recovering its restriction state.", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_fail_closed_until_external_recovery_completes() => _admitted.ShouldEqual(new[] { HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError });
    [Fact] void should_limit_both_paths_in_the_production_pipeline() => _limited.ShouldEqual(new[] { HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests });
    [Fact] void should_not_enter_openiddict_authentication_when_over_budget() => _authenticationRequests.ShouldEqual(_admittedAuthenticationRequests);
    [Fact] void should_return_the_limiter_error_for_both_paths() => _errors.TrueForAll(error => error.Contains("\"error\":\"temporarily_unavailable\"", StringComparison.Ordinal)).ShouldBeTrue();
}
