// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_without_a_presenter : given.a_polling_request
{
    void Establish() => _context.DeviceCodePrincipal.SetPresenters(Array.Empty<string>());

    async Task Because() => await _guard.HandleAsync(_context);

    [Fact] void should_not_write_polling_state() => _store.Writes.ShouldEqual(0);
}
