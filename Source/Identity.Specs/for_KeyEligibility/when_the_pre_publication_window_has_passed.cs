// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;

namespace Cratis.Identity.for_KeyEligibility;

public class when_the_pre_publication_window_has_passed : given.keys_at_a_moment
{
    void Because() => Select(
        RealmKeys.Signing("old", Ago(TimeSpan.FromDays(30)), Ago(TimeSpan.FromDays(-30))),
        RealmKeys.Signing("new", Ago(TimeSpan.FromSeconds(600)), Ago(TimeSpan.FromDays(-60))));

    [Fact] void should_sign_with_the_successor() => _current.Kid.ShouldEqual("new");
}
