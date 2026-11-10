// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_two_replicas_share_a_budget : given.a_shared_limiter
{
    void Establish() => _options.DeviceGrant.AttemptsPerAccount = 3;

    async Task Because() => _results = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => (index % 2 == 0 ? _first : _second).Verify("BCDFGHJK", "account", "192.0.2.1", CancellationToken.None)));

    [Fact] void should_admit_only_one_shared_budget() => _results.Count(_ => _).ShouldEqual(3);
    [Fact] void should_reject_all_other_attempts() => _results.Count(_ => !_).ShouldEqual(5);
}
