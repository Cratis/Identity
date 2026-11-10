// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_accounts_are_rotated_against_one_code : given.a_shared_limiter
{
    async Task Because()
    {
        _results = new bool[6];
        for (var index = 0; index < _results.Length; index++) _results[index] = await Verify(account: $"account-{index}", ip: $"192.0.2.{index + 1}");
    }

    [Fact] void should_admit_exactly_the_per_code_budget() => _results.Count(_ => _).ShouldEqual(5);
    [Fact] void should_not_reset_the_code_budget_for_a_new_account_and_ip() => _results[5].ShouldBeFalse();
    [Fact] void should_not_persist_raw_codes_or_identifiers() => _store.Rows.Keys.All(_ => !_.Contains("BCDFGHJK", StringComparison.Ordinal) && !_.Contains("account-", StringComparison.Ordinal) && !_.Contains("192.0.2.", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_actually_persist_operational_windows() => _store.Rows.Count.ShouldEqual(14);
}
