// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_primary_storage_is_unavailable : given.a_shared_limiter
{
    bool? _poll;
    void Establish() => _store.Unavailable = true;
    async Task Because()
    {
        _results = [await Verify()];
        _poll = await _second.Poll("opaque-device-test-value", CancellationToken.None);
    }

    [Fact] void should_deny_verification() => _results[0].ShouldBeFalse();
    [Fact] void should_deny_polling_instead_of_falling_back_to_memory() => _poll.ShouldBeNull();
}
