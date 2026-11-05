// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_is_slower_than_its_timeout : given.a_tenants_callback
{
    Stopwatch _elapsed;

    void Establish()
    {
        // This double is slow on purpose so the callback deadline cancels it.
        handler.BeforeResponse = token => Task.Delay(Timeout.InfiniteTimeSpan, token);
    }

    async Task Because()
    {
        _elapsed = Stopwatch.StartNew();
        await Lookup();
        _elapsed.Stop();
    }

    [Fact] void should_time_out() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.TimedOut));
    [Fact] void should_bound_the_wait() => (_elapsed.Elapsed < TimeSpan.FromSeconds(1)).ShouldBeTrue();
}
