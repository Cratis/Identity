// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_all_guessing_dimensions_rotate : given.a_shared_limiter
{
    void Establish() => _options.DeviceGrant.AttemptsGlobally = 2;

    async Task Because() => _results =
    [
        await Verify("BCDFGHJK", "first", "192.0.2.1"),
        await _second.Verify("BCDFGHJL", "second", "192.0.2.2", CancellationToken.None),
        await Verify("BCDFGHJM", "third", "192.0.2.3")
    ];

    [Fact] void should_admit_the_global_allowance() => _results.Take(2).All(_ => _).ShouldBeTrue();
    [Fact] void should_reject_the_next_guess_despite_rotated_code_account_and_ip() => _results[2].ShouldBeFalse();
}
