// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Keys;

namespace Cratis.Identity.for_KeyEligibility;

public class when_two_eligible_keys_start_together : given.keys_at_a_moment
{
    RealmKey _first;

    void Because()
    {
        var start = Ago(TimeSpan.FromHours(1));
        var end = Ago(TimeSpan.FromDays(-10));
        var b = RealmKeys.Signing("b", start, end);
        var a = RealmKeys.Signing("a", start, end);
        Select(b, a);
        _first = _current;
        Select(a, b);
    }

    [Fact] void should_choose_the_lowest_kid() => _current.Kid.ShouldEqual("a");
    [Fact] void should_not_depend_on_the_order_of_the_files() => _first.Kid.ShouldEqual(_current.Kid);
}
