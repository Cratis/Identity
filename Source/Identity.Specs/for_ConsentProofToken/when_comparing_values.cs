// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow;

namespace Cratis.Identity.for_ConsentProofToken;

public class when_comparing_values : Specification
{
    ConsentProofToken _value;

    void Because() => _value = new("protected-test-value");

    [Fact] void should_compare_by_value() => _value.ShouldEqual(new ConsentProofToken("protected-test-value"));
    [Fact] void should_convert_to_the_primitive() => ((string)_value).ShouldEqual("protected-test-value");
    [Fact] void should_round_trip_the_value() => new ConsentProofToken(_value.Value).ShouldEqual(_value);
    [Fact] void should_preserve_an_absent_proof() => ConsentProofToken.From(null).ShouldBeNull();
    [Fact] void should_preserve_an_empty_proof() => ConsentProofToken.From(string.Empty).Value.ShouldEqual(string.Empty);
}
