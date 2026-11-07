// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_code_and_account_rotate_on_one_ip : given.a_shared_limiter
{
    void Establish() => _options.DeviceGrant.AttemptsPerIp = 2;

    async Task Because()
    {
        _results = [await Verify(account: "first"), await Verify("BCDFGHJL", "second"), await Verify("BCDFGHJM", "third")];
    }

    [Fact] void should_admit_the_secondary_ip_budget() => _results.Take(2).All(_ => _).ShouldBeTrue();
    [Fact] void should_reject_the_next_attempt() => _results[2].ShouldBeFalse();
}
