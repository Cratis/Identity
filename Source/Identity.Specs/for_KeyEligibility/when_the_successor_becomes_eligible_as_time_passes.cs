// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Keys;

namespace Cratis.Identity.for_KeyEligibility;

public class when_the_successor_becomes_eligible_as_time_passes : given.keys_at_a_moment
{
    RealmKey _before;
    RealmKey _after;
    DateTimeOffset? _boundary;

    void Because()
    {
        var keys = new[]
        {
            RealmKeys.Signing("old", Ago(TimeSpan.FromDays(30)), Ago(TimeSpan.FromDays(-30))),
            RealmKeys.Signing("new", Ago(TimeSpan.FromMinutes(5)), Ago(TimeSpan.FromDays(-60)))
        };
        _before = _eligibility.Current(KeyPurpose.Signing, keys);
        _boundary = _eligibility.NextBoundary(keys);
        _time.Now = KeyMaterial.Now + TimeSpan.FromMinutes(5);
        _after = _eligibility.Current(KeyPurpose.Signing, keys);
    }

    [Fact] void should_sign_with_the_predecessor_first() => _before.Kid.ShouldEqual("old");
    [Fact] void should_switch_to_the_successor_after_the_window() => _after.Kid.ShouldEqual("new");
    [Fact] void should_report_the_end_of_the_window_as_the_next_boundary() => _boundary.ShouldEqual(KeyMaterial.Now + TimeSpan.FromMinutes(5));
}
