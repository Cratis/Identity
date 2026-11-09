// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_no_cas_can_be_won : given.a_shared_limiter
{
    bool? _poll;
    int _verificationWrites;
    void Establish() => _store.Contended = true;
    async Task Because()
    {
        _results = [await Verify()];
        _verificationWrites = _store.Writes;
        _poll = await _first.Poll("opaque-device-test-value", CancellationToken.None);
    }

    [Fact] void should_use_atomic_increment_for_verification() => _results[0].ShouldBeTrue();
    [Fact] void should_fail_polling_closed() => _poll.ShouldBeNull();
    [Fact] void should_increment_all_four_verification_budgets_once() => _verificationWrites.ShouldEqual(4);
    [Fact] void should_bound_polling_retries() => (_store.Writes - _verificationWrites).ShouldEqual(8);
}
