// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;

namespace Cratis.Identity.for_KeyEligibility;

public class when_there_is_a_single_key_that_has_just_started : given.keys_at_a_moment
{
    void Because() => Select(RealmKeys.Signing("only", Ago(TimeSpan.FromMinutes(1)), Ago(TimeSpan.FromDays(-30))));

    [Fact] void should_sign_with_it_because_there_is_no_predecessor() => _current.Kid.ShouldEqual("only");
}
