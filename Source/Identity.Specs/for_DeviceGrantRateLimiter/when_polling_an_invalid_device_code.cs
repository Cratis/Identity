// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_an_invalid_device_code : given.a_polling_request
{
    void Establish() => _context.Principal = null;

    async Task Because() => await _guard.HandleAsync(_context);

    [Fact] void should_not_write_polling_state() => _store.Writes.ShouldEqual(0);
    [Fact] void should_not_create_a_window_for_the_untrusted_code() => _store.Rows.Count.ShouldEqual(0);
}
