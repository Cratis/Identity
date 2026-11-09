// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRequestRateLimiter;

public class when_a_refused_flood_continues : Specification
{
    int _retry;
    int _minimum;
    bool _reset;

    void Because()
    {
        var time = new FixedTimeProvider(DateTimeOffset.UtcNow);
        var limiter = new TokenRequestRateLimiter(time, Options.Create(new IdentityOptions { TokenRateLimit = new() { PerSourceRequests = 1, Window = TimeSpan.FromMilliseconds(2500) } }), NullLogger<TokenRequestRateLimiter>.Instance);
        limiter.Acquire(null, null, "source");
        time.Now += TimeSpan.FromMilliseconds(1001);
        for (var index = 0; index < 1000; index++) _retry = limiter.Acquire($"forged-{index}", null, "source").RetryAfter;
        time.Now += TimeSpan.FromMilliseconds(1498);
        _minimum = limiter.Acquire(null, null, "source").RetryAfter;
        time.Now += TimeSpan.FromMilliseconds(1);
        _reset = limiter.Acquire(null, null, "source").Allowed;
    }

    [Fact] void should_round_up_the_remaining_window_not_restart_it() => _retry.ShouldEqual(2);
    [Fact] void should_return_at_least_one_second_before_expiry() => _minimum.ShouldEqual(1);
    [Fact] void should_admit_at_the_exact_window_boundary() => _reset.ShouldBeTrue();
}
