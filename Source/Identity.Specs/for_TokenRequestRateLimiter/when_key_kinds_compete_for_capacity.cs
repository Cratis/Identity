// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRequestRateLimiter;

public class when_key_kinds_compete_for_capacity : Specification
{
    int _tracked;
    bool _newSourceAllowed;
    bool _newSourceLimited;
    bool _newSourceExchangeAllowed;
    bool _newSourceExchangeLimited;
    bool _establishedAllowed;
    bool _unknownSourceLimited;
    bool _recovered;

    void Because()
    {
        var time = new FixedTimeProvider(DateTimeOffset.UtcNow);
        var limiter = new TokenRequestRateLimiter(time, Options.Create(new IdentityOptions { TokenRateLimit = new() { MaxTrackedKeys = 100, PerSourceRequests = 1000, PerClientRequests = 2, PerClientExchangeRequests = 1 } }), NullLogger<TokenRequestRateLimiter>.Instance);
        limiter.Acquire("established", "refresh_token", "legitimate");
        for (var index = 0; index < 120; index++)
        {
            limiter.Acquire($"forged-{index}", TokenExchangeConstants.GrantType, "attacker");
        }

        _tracked = limiter.TrackedCount;

        // A full client/exchange table uses source-local overflow; it must not use
        // an attacker-exhausted global overflow or consume source-table capacity.
        _newSourceAllowed = limiter.Acquire("new", "refresh_token", "new-source").Allowed;
        limiter.Acquire("rotated", "refresh_token", "new-source");
        _newSourceLimited = !limiter.Acquire("another", "refresh_token", "new-source").Allowed;
        _newSourceExchangeAllowed = limiter.Acquire("new", TokenExchangeConstants.GrantType, "exchange-source").Allowed;
        _newSourceExchangeLimited = !limiter.Acquire("rotated", TokenExchangeConstants.GrantType, "exchange-source").Allowed;
        _establishedAllowed = limiter.Acquire("established", "refresh_token", "legitimate").Allowed;
        time.Now += TimeSpan.FromMinutes(2);
        _recovered = limiter.Acquire("new", TokenExchangeConstants.GrantType, "new-source").Allowed && limiter.TrackedCount == 3;

        var sources = new TokenRequestRateLimiter(time, Options.Create(new IdentityOptions { TokenRateLimit = new() { MaxTrackedKeys = 100, PerSourceRequests = 1 } }), NullLogger<TokenRequestRateLimiter>.Instance);
        for (var index = 0; index < 101; index++) sources.Acquire(null, null, $"source-{index}");
        _unknownSourceLimited = !sources.Acquire(null, null, "rotated-source").Allowed;
    }

    [Fact] void should_bound_each_kind_independently() => _tracked.ShouldEqual(202);
    [Fact] void should_reserve_source_capacity_despite_client_and_exchange_rotation() => _newSourceAllowed.ShouldBeTrue();
    [Fact] void should_limit_new_client_keys_in_source_local_overflow() => _newSourceLimited.ShouldBeTrue();
    [Fact] void should_not_share_exchange_overflow_between_sources() => _newSourceExchangeAllowed.ShouldBeTrue();
    [Fact] void should_limit_new_exchange_keys_in_source_local_overflow() => _newSourceExchangeLimited.ShouldBeTrue();
    [Fact] void should_not_evict_live_entries() => _establishedAllowed.ShouldBeTrue();
    [Fact] void should_reclaim_all_key_kinds_after_expiry() => _recovered.ShouldBeTrue();
    [Fact] void should_fail_closed_in_shared_overflow_when_the_source_table_is_full() => _unknownSourceLimited.ShouldBeTrue();
}
