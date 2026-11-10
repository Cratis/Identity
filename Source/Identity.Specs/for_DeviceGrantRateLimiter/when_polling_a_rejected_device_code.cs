// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_a_rejected_device_code : given.a_polling_request
{
    void Establish() => _context.Reject("expired_token");

    async Task Because() => await _guard.HandleAsync(_context);

    [Fact] void should_not_write_polling_state() => _store.Writes.ShouldEqual(0);
    [Fact] void should_preserve_the_validation_error() => _context.Error.ShouldEqual("expired_token");
}
