// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_an_unknown_token_entry : given.a_polling_request
{
    void Establish() => _tokens.FindByIdAsync("validated-device-entry", Arg.Any<CancellationToken>()).Returns((object)null);

    async Task Because() => await _guard.HandleAsync(_context);

    [Fact] void should_not_write_polling_state() => _store.Writes.ShouldEqual(0);
    [Fact] void should_leave_the_validation_error_to_openiddict() => _context.IsRejected.ShouldBeFalse();
}
