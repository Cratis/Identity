// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_client_ip_cannot_be_established : given.a_shared_limiter
{
    async Task Because() => _results = [await _first.Verify("BCDFGHJK", "account", null, CancellationToken.None)];

    [Fact] void should_fail_closed() => _results[0].ShouldBeFalse();
    [Fact] void should_not_fall_back_to_a_shared_unknown_ip() => _store.Writes.ShouldEqual(0);
}
