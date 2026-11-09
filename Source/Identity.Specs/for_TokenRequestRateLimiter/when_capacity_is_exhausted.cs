// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRequestRateLimiter;

public class when_capacity_is_exhausted : Specification
{
    FixedTimeProvider _time;
    TokenRequestRateLimiter _limiter;
    int _tracked;
    bool _overflowLimited;
    bool _establishedAllowed;
    bool _establishedLimited;
    bool _recovered;
    int _afterSweep;

    void Establish()
    {
        _time = new(DateTimeOffset.UtcNow);
        _limiter = new(_time, Options.Create(new IdentityOptions { TokenRateLimit = new() { MaxTrackedKeys = 100, PerClientRequests = 2, PerSourceRequests = 1000 } }), Substitute.For<ILogger<TokenRequestRateLimiter>>());
    }

    void Because()
    {
        _limiter.Acquire("established", null, "source");
        for (var index = 0; index < 300; index++)
        {
            _overflowLimited = !_limiter.Acquire($"client-{index}", null, "source").Allowed;
        }

        _tracked = _limiter.TrackedCount;
        _establishedAllowed = _limiter.Acquire("established", null, "source").Allowed;
        _establishedLimited = !_limiter.Acquire("established", null, "source").Allowed;
        _time.Now += TimeSpan.FromMinutes(2);
        _recovered = _limiter.Acquire("new-client", null, "new-source").Allowed;
        _afterSweep = _limiter.TrackedCount;
    }

    [Fact] void should_keep_a_hard_capacity() => _tracked.ShouldEqual(101);
    [Fact] void should_limit_overflow_clients_together() => _overflowLimited.ShouldBeTrue();
    [Fact] void should_not_evict_live_clients() => _establishedAllowed.ShouldBeTrue();
    [Fact] void should_preserve_the_live_client_budget() => _establishedLimited.ShouldBeTrue();
    [Fact] void should_recover_capacity() => _recovered.ShouldBeTrue();
    [Fact] void should_sweep_expired_entries() => _afterSweep.ShouldEqual(2);
}
