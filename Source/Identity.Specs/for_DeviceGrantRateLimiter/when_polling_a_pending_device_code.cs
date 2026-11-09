// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_polling_a_pending_device_code : given.a_polling_request
{
    string _firstError;

    async Task Because()
    {
        await _guard.HandleAsync(_context);
        await new OpenIddictServerHandlers.Protection.ValidateTokenEntry(_tokens).HandleAsync(_context);
        _firstError = _context.Error;
        _context = new ValidateTokenContext(_context.Transaction)
        {
            Principal = _context.Principal,
            TokenId = _context.TokenId,
            Token = "another-opaque-representation"
        };
        await _guard.HandleAsync(_context);
    }

    [Fact] void should_leave_the_first_poll_pending() => _firstError.ShouldEqual("authorization_pending");
    [Fact] void should_slow_down_the_early_repoll() => _context.Error.ShouldEqual("slow_down");
    [Fact] void should_share_one_window_for_the_validated_entry() => _store.Rows.Count.ShouldEqual(1);
    [Fact] void should_increase_the_interval_by_five_seconds() => _store.Rows.Values.Single().Interval.ShouldEqual(10);
}
