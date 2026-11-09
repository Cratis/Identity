// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_an_ip_budget_refuses_verification : given.a_shared_limiter
{
    bool _refused;
    bool _legitimate;

    async Task Establish()
    {
        _options.DeviceGrant.AttemptsPerIp = 1;
        _options.DeviceGrant.AttemptsPerCode = 2;
        _options.DeviceGrant.AttemptsGlobally = 2;
        await Verify();
    }

    async Task Because()
    {
        _refused = await Verify();
        _legitimate = await Verify(account: "another-account", ip: "192.0.2.2");
    }

    [Fact] void should_refuse_the_exhausted_ip() => _refused.ShouldBeFalse();
    [Fact] void should_preserve_code_and_global_capacity_for_another_caller() => _legitimate.ShouldBeTrue();
    [Fact] void should_only_increment_the_ip_counter_for_the_refused_request() => _store.Writes.ShouldEqual(9);
}
