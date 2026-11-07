// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Device;
using OpenIddict.Server;

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_a_validated_device_code : given.a_polling_request
{
    async Task Because()
    {
        await _guard.HandleAsync(_context);
        _context.DeviceCode = "another-opaque-representation";
        await _guard.HandleAsync(_context);
    }

    [Fact] void should_share_the_window_for_the_validated_token_entry() => _store.Rows.Count.ShouldEqual(1);
    [Fact] void should_slow_down_the_early_poll() => _context.Error.ShouldEqual("slow_down");
    [Fact] void should_increase_the_interval_by_five_seconds() => _store.Rows.Values.Single().Interval.ShouldEqual(10);
    [Fact] void should_run_after_device_code_validation() => DevicePollingGuard.Descriptor.Order.ShouldEqual(OpenIddictServerHandlers.ValidateDeviceCode.Descriptor.Order + 1);
    [Fact] void should_check_availability_before_device_code_validation() => DevicePollingAdmissionGuard.Descriptor.Order.ShouldEqual(OpenIddictServerHandlers.ValidateDeviceCode.Descriptor.Order - 1);
}
