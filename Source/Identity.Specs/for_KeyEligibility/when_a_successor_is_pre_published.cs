// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;

namespace Cratis.Identity.for_KeyEligibility;

public class when_a_successor_is_pre_published : given.keys_at_a_moment
{
    void Because() => Select(
        RealmKeys.Signing("old", Ago(TimeSpan.FromDays(30)), Ago(TimeSpan.FromDays(-30))),
        RealmKeys.Signing("new", Ago(TimeSpan.FromMinutes(5)), Ago(TimeSpan.FromDays(-60))));

    [Fact] void should_keep_signing_with_the_predecessor() => _current.Kid.ShouldEqual("old");
}
