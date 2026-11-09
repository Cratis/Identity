// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_a_window_expires_before_ttl_cleanup : given.a_shared_limiter
{
    void Establish() => _options.DeviceGrant.AttemptsPerAccount = 1;
    async Task Because()
    {
        var admitted = await Verify();
        var exhausted = await Verify();
        _clock.Now += _options.DeviceGrant.VerificationWindow;
        _results = [admitted, exhausted, await Verify()];
    }

    [Fact] void should_first_exhaust_a_real_window() => _results[1].ShouldBeFalse();
    [Fact] void should_admit_the_initial_request() => _results[0].ShouldBeTrue();
    [Fact] void should_use_logical_expiry_without_waiting_for_mongo_ttl() => _results[2].ShouldBeTrue();
    [Fact] void should_replace_not_accumulate_windows() => _store.Rows.Count.ShouldEqual(4);
}
