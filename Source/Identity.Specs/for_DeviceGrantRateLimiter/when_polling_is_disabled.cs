// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Device;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_is_disabled : given.a_polling_admission_request
{
    async Task Because() => await new DevicePollingAdmissionGuard(_admission, Options.Create(new IdentityOptions())).HandleAsync(_context);

    [Fact] void should_fail_closed_before_validation() => _context.Error.ShouldEqual("temporarily_unavailable");
    [Fact] void should_preserve_the_availability_message() => _context.ErrorDescription.ShouldEqual("Device authorization is unavailable.");
}
