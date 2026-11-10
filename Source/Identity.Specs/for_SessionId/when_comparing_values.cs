// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Sessions;

namespace Cratis.Identity.for_SessionId;

public class when_comparing_values : Specification
{
    SessionId _value;

    void Because() => _value = new("session");

    [Fact] void should_compare_by_value() => _value.ShouldEqual(new SessionId("session"));
    [Fact] void should_convert_to_the_primitive() => ((string)_value).ShouldEqual("session");
    [Fact] void should_round_trip_the_value() => new SessionId(_value.Value).ShouldEqual(_value);
}
