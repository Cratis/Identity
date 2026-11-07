// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_codes_are_rotated_against_one_account : given.a_shared_limiter
{
    void Establish() => _options.DeviceGrant.AttemptsPerAccount = 3;

    async Task Because()
    {
        _results = new bool[4];
        var codes = new[] { "BCDFGHJK", "BCDFGHJL", "BCDFGHJM", "BCDFGHJN" };
        for (var index = 0; index < codes.Length; index++) _results[index] = await Verify(codes[index], ip: $"192.0.2.{index + 1}");
    }

    [Fact] void should_admit_the_budgeted_attempts() => _results.Take(3).All(_ => _).ShouldBeTrue();
    [Fact] void should_not_reset_the_account_budget_for_a_new_code_and_ip() => _results[3].ShouldBeFalse();
}
