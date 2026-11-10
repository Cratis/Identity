// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Resources;

namespace Cratis.Identity.for_Scope;

public class when_comparing_values : Specification
{
    Scope _value;

    void Because() => _value = new("api:read");

    [Fact] void should_compare_by_value() => _value.ShouldEqual(new Scope("api:read"));
    [Fact] void should_convert_to_the_primitive() => ((string)_value).ShouldEqual("api:read");
    [Fact] void should_round_trip_the_value() => new Scope(_value.Value).ShouldEqual(_value);
}
