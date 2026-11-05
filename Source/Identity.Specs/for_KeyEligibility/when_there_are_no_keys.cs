// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Keys;

namespace Cratis.Identity.for_KeyEligibility;

public class when_there_are_no_keys : given.keys_at_a_moment
{
    void Because() => Select();

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<NoUsableKey>();
}
