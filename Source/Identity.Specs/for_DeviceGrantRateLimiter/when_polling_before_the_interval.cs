// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_before_the_interval : given.a_shared_limiter
{
    bool?[] _polls;
    int _increasedInterval;
    async Task Because()
    {
        var first = await _first.Poll("opaque-device-test-value", CancellationToken.None);
        _clock.Now = _clock.Now.AddSeconds(1);
        var second = await _second.Poll("opaque-device-test-value", CancellationToken.None);
        _clock.Now = _clock.Now.AddSeconds(5);
        var third = await _first.Poll("opaque-device-test-value", CancellationToken.None);
        _increasedInterval = _store.Rows.Values.Single().Interval;
        _clock.Now = _clock.Now.AddSeconds(15);
        _polls = [first, second, third, await _second.Poll("opaque-device-test-value", CancellationToken.None)];
    }

    [Fact] void should_admit_the_first_poll() => _polls[0].ShouldEqual(false);
    [Fact] void should_slow_the_early_poll_on_another_replica() => _polls[1].ShouldEqual(true);
    [Fact] void should_retain_the_increased_interval() => _polls[2].ShouldEqual(true);
    [Fact] void should_add_five_seconds_for_each_early_poll() => _increasedInterval.ShouldEqual(15);
    [Fact] void should_admit_after_the_increased_interval() => _polls[3].ShouldEqual(false);
}
