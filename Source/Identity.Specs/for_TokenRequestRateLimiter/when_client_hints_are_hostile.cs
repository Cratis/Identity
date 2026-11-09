// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRequestRateLimiter;

public class when_client_hints_are_hostile : Specification
{
    bool _victimAllowed;
    bool _victimExchangeAllowed;
    bool _rotationRefused;
    bool _clientUnspent;
    bool _exchangeUnspent;
    int _beforeRefusals;
    int _afterRefusals;

    void Because()
    {
        var spoofing = Create(new() { PerClientRequests = 1, PerClientExchangeRequests = 1 });
        spoofing.Acquire("victim", TokenExchangeConstants.GrantType, "attacker");
        _victimAllowed = spoofing.Acquire("victim", "refresh_token", "legitimate").Allowed;
        _victimExchangeAllowed = spoofing.Acquire("victim", TokenExchangeConstants.GrantType, "another-legitimate").Allowed;

        var source = Create(new() { PerSourceRequests = 1 });
        source.Acquire(null, null, "attacker");
        _beforeRefusals = source.TrackedCount;
        for (var index = 0; index < 200; index++)
        {
            _rotationRefused = !source.Acquire($"forged-{index}", TokenExchangeConstants.GrantType, "attacker").Allowed;
        }

        _afterRefusals = source.TrackedCount;

        // Move only the source window past expiry. A refused request must not have started
        // a later client or exchange window that now blocks otherwise admissible work.
        var time = new FixedTimeProvider(DateTimeOffset.UtcNow);
        var ordering = Create(new() { PerSourceRequests = 1, PerClientRequests = 1, PerClientExchangeRequests = 1 }, time);
        ordering.Acquire(null, null, "shared");
        time.Now += TimeSpan.FromSeconds(30);
        ordering.Acquire("target", TokenExchangeConstants.GrantType, "shared");
        time.Now += TimeSpan.FromSeconds(31);
        _clientUnspent = ordering.Acquire("target", "refresh_token", "shared").Allowed;

        var exchangeTime = new FixedTimeProvider(DateTimeOffset.UtcNow);
        var exchange = Create(new() { PerSourceRequests = 1, PerClientExchangeRequests = 1 }, exchangeTime);
        exchange.Acquire(null, null, "shared");
        exchangeTime.Now += TimeSpan.FromSeconds(30);
        exchange.Acquire("target", TokenExchangeConstants.GrantType, "shared");
        exchangeTime.Now += TimeSpan.FromSeconds(31);
        _exchangeUnspent = exchange.Acquire("target", TokenExchangeConstants.GrantType, "shared").Allowed;
    }

    static TokenRequestRateLimiter Create(TokenRateLimitOptions options, TimeProvider? time = null) => new(time ?? TimeProvider.System, Options.Create(new IdentityOptions { TokenRateLimit = options }), NullLogger<TokenRequestRateLimiter>.Instance);

    [Fact] void should_not_let_forged_ids_spend_another_sources_client_budget() => _victimAllowed.ShouldBeTrue();
    [Fact] void should_not_let_forged_ids_spend_another_sources_exchange_budget() => _victimExchangeAllowed.ShouldBeTrue();
    [Fact] void should_keep_the_source_budget_authoritative_despite_rotating_ids() => _rotationRefused.ShouldBeTrue();
    [Fact] void should_not_allocate_client_or_exchange_keys_for_source_refusals() => _afterRefusals.ShouldEqual(_beforeRefusals);
    [Fact] void should_not_charge_client_counters_after_a_source_refusal() => _clientUnspent.ShouldBeTrue();
    [Fact] void should_not_charge_exchange_counters_after_a_source_refusal() => _exchangeUnspent.ShouldBeTrue();
}
