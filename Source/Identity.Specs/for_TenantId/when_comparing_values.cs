// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_TenantId;

public class when_comparing_values : Specification
{
    TenantId _value;

    void Because() => _value = new("tenant");

    [Fact] void should_compare_by_value() => _value.ShouldEqual(new TenantId("tenant"));
    [Fact] void should_convert_to_the_primitive() => ((string)_value).ShouldEqual("tenant");
    [Fact] void should_round_trip_the_value() => new TenantId(_value.Value).ShouldEqual(_value);
}
