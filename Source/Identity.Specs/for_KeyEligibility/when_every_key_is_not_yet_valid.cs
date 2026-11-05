// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Keys;

namespace Cratis.Identity.for_KeyEligibility;

public class when_every_key_is_not_yet_valid : given.keys_at_a_moment
{
    void Because() => Select(RealmKeys.Signing("future", Ago(TimeSpan.FromSeconds(-30)), Ago(TimeSpan.FromDays(-60))));

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<NoUsableKey>();
}
