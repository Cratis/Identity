// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Device;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_the_first_counter_refuses : Specification
{
    IDeviceGrantStateStore _store;
    bool _result;

    void Establish()
    {
        _store = Substitute.For<IDeviceGrantStateStore>();
        _store.Increment(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(2);
    }

    async Task Because()
    {
        var options = new IdentityOptions();
        options.DeviceGrant.AttemptsPerIp = 1;
        var limiter = new DeviceGrantRateLimiter(_store, Options.Create(options), TimeProvider.System, NullLogger<DeviceGrantRateLimiter>.Instance);
        _result = await limiter.Verify("BCDFGHJK", "account", "192.0.2.1", CancellationToken.None);
    }

    [Fact]
    void should_refuse_the_request() => _result.ShouldBeFalse();

    [Fact]
    void should_never_increment_account_code_or_global_counters() => _store.ReceivedCalls().Count().ShouldEqual(1);
}
