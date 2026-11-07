// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_issuance_cannot_be_accounted_for : given.a_shared_limiter
{
    async Task Because()
    {
        var noIp = await _first.Issue(null, CancellationToken.None);
        var blankIp = await _second.Issue(string.Empty, CancellationToken.None);
        _store.Unavailable = true;
        _results = [noIp, blankIp, await _first.Issue("192.0.2.1", CancellationToken.None)];
    }

    [Fact] void should_fail_all_three_requests_closed() => _results.ShouldEqual([false, false, false]);
}
