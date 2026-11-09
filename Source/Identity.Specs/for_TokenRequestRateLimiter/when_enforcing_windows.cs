// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRequestRateLimiter;

public class when_enforcing_windows : Specification
{
    FixedTimeProvider _time;
    TokenRequestRateLimiter _limiter;
    bool _first;
    bool _limited;
    bool _other;
    bool _reset;
    bool _sourceLimited;
    bool _exchangeLimited;
    bool _generalAllowed;
    bool _disabled;
    int _retry;

    void Establish()
    {
        _time = new(DateTimeOffset.UtcNow);
        _limiter = Create(new() { PerClientRequests = 1, Window = TimeSpan.FromMilliseconds(1500) });
    }

    void Because()
    {
        _first = _limiter.Acquire("a", null, "source").Allowed;
        var refusal = _limiter.Acquire("a", null, "source");
        _limited = !refusal.Allowed;
        _retry = refusal.RetryAfter;
        _other = _limiter.Acquire("b", null, "source").Allowed;
        _time.Now += TimeSpan.FromSeconds(2);
        _reset = _limiter.Acquire("a", null, "source").Allowed;
        var source = Create(new() { PerSourceRequests = 1 });
        source.Acquire("a", null, "shared");
        _sourceLimited = !source.Acquire("b", null, "shared").Allowed;
        var exchange = Create(new() { PerClientExchangeRequests = 1 });
        exchange.Acquire("a", TokenExchangeConstants.GrantType, "source");
        _exchangeLimited = !exchange.Acquire("a", TokenExchangeConstants.GrantType, "source").Allowed;
        _generalAllowed = exchange.Acquire("a", "refresh_token", "source").Allowed;
        var disabled = Create(new() { Enabled = false, PerClientRequests = 0, PerSourceRequests = 0 });
        _disabled = disabled.Acquire("a", null, "source").Allowed;
    }

    TokenRequestRateLimiter Create(TokenRateLimitOptions options) => new(_time, Options.Create(new IdentityOptions { TokenRateLimit = options }), Substitute.For<ILogger<TokenRequestRateLimiter>>());

    [Fact] void should_allow_the_budget() => _first.ShouldBeTrue();
    [Fact] void should_refuse_the_next_request() => _limited.ShouldBeTrue();
    [Fact] void should_leave_other_clients_unaffected() => _other.ShouldBeTrue();
    [Fact] void should_reset_after_the_window() => _reset.ShouldBeTrue();
    [Fact] void should_limit_a_source_across_client_ids() => _sourceLimited.ShouldBeTrue();
    [Fact] void should_limit_exchange_separately() => _exchangeLimited.ShouldBeTrue();
    [Fact] void should_leave_other_grants_the_general_budget() => _generalAllowed.ShouldBeTrue();
    [Fact] void should_allow_when_disabled() => _disabled.ShouldBeTrue();
    [Fact] void should_round_up_the_full_window() => _retry.ShouldEqual(2);
}
