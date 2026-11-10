// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Realm;

namespace Cratis.Identity.for_RealmId;

public class when_comparing_values : Specification
{
    RealmId _value;

    void Because() => _value = new("https://identity.example");

    [Fact] void should_compare_by_value() => _value.ShouldEqual(new RealmId("https://identity.example"));
    [Fact] void should_convert_to_the_primitive() => ((string)_value).ShouldEqual("https://identity.example");
    [Fact] void should_round_trip_the_value() => new RealmId(_value.Value).ShouldEqual(_value);
}
