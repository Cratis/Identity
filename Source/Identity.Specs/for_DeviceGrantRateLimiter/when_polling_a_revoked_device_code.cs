// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_a_revoked_device_code : given.a_polling_request
{
    void Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Inactive, Arg.Any<CancellationToken>()).Returns(false);
        _tokens.HasStatusAsync(_token, Statuses.Revoked, Arg.Any<CancellationToken>()).Returns(true);
    }

    async Task Because() => await _guard.HandleAsync(_context);

    [Fact] void should_not_write_polling_state() => _store.Writes.ShouldEqual(0);
    [Fact] void should_leave_the_status_rejection_to_openiddict() => _context.IsRejected.ShouldBeFalse();
}
