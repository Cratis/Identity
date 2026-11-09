// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRequestRateLimiter;

public class when_requests_arrive_concurrently : Specification
{
    int _allowed;

    void Because()
    {
        var limiter = new TokenRequestRateLimiter(new FixedTimeProvider(DateTimeOffset.UtcNow), Options.Create(new IdentityOptions { TokenRateLimit = new() { PerClientRequests = 10, PerSourceRequests = 1000 } }), Substitute.For<ILogger<TokenRequestRateLimiter>>());
        Parallel.For(0, 100, _ =>
        {
            if (limiter.Acquire("client", null, "source").Allowed)
            {
                Interlocked.Increment(ref _allowed);
            }
        });
    }

    [Fact] void should_admit_exactly_the_budget() => _allowed.ShouldEqual(10);
}
