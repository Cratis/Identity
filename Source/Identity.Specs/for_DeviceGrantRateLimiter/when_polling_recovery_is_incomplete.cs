// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Device;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_recovery_is_incomplete : given.a_polling_admission_request
{
    void Establish() => _gate.Close();

    async Task Because() => await new DevicePollingAdmissionGuard(_admission, Options.Create(new IdentityOptions
    {
        DeviceGrant = new() { Enabled = true }
    })).HandleAsync(_context);

    [Fact] void should_fail_closed_before_validation() => _context.Error.ShouldEqual("temporarily_unavailable");
}
