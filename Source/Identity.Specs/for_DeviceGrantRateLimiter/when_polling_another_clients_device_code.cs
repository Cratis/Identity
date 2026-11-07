// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_another_clients_device_code : given.a_polling_request
{
    void Establish() => _context.Principal.SetPresenters("another-client");

    async Task Because() => await _guard.HandleAsync(_context);

    [Fact] void should_not_write_polling_state() => _store.Writes.ShouldEqual(0);
    [Fact] void should_leave_the_authorized_party_rejection_to_openiddict() => _context.IsRejected.ShouldBeFalse();
}
