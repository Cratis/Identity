// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;

namespace Cratis.Identity.for_ClientId;

public class when_comparing_values : Specification
{
    ClientId _value;

    void Because() => _value = new("client");

    [Fact] void should_compare_by_value() => _value.Equals(new ClientId("client")).ShouldBeTrue();
    [Fact] void should_preserve_ordinal_comparison() => _value.Equals(new ClientId("CLIENT")).ShouldBeFalse();
    [Fact] void should_have_equal_hash_codes() => _value.GetHashCode().ShouldEqual(new ClientId("client").GetHashCode());
    [Fact] void should_convert_to_the_primitive() => ((string)_value).ShouldEqual("client");
    [Fact] void should_round_trip_the_value() => new ClientId(_value.Value).ShouldEqual(_value);
    [Fact] void should_preserve_an_absent_hint() => ClientId.From(null).ShouldBeNull();
    [Fact] void should_preserve_an_empty_hint() => ClientId.From(string.Empty).Value.ShouldEqual(string.Empty);
}
