// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_issuing_from_two_replicas : given.a_shared_limiter
{
    bool _afterWindow;
    bool _anotherIp;
    void Establish() => _options.DeviceGrant.IssuancesPerIp = 2;

    async Task Because()
    {
        _results = [await _first.Issue("192.0.2.1", CancellationToken.None), await _second.Issue("192.0.2.1", CancellationToken.None), await _first.Issue("192.0.2.1", CancellationToken.None)];
        _anotherIp = await _second.Issue("192.0.2.2", CancellationToken.None);
        _clock.Now = _clock.Now.AddMinutes(1);
        _afterWindow = await _second.Issue("192.0.2.1", CancellationToken.None);
    }

    [Fact] void should_share_the_issuance_allowance() => _results.ShouldEqual([true, true, false]);
    [Fact] void should_keep_other_ips_independent() => _anotherIp.ShouldBeTrue();
    [Fact] void should_restart_at_logical_expiry_without_waiting_for_ttl_cleanup() => _afterWindow.ShouldBeTrue();
}
