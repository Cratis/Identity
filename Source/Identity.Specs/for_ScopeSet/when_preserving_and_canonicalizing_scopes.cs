// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Resources;

namespace Cratis.Identity.for_ScopeSet;

public class when_preserving_and_canonicalizing_scopes : Specification
{
    ScopeSet _value;
    ScopeSet _canonical;

    void Because()
    {
        _value = new("write  read write");
        _canonical = ScopeSet.From(_value.Scopes);
    }

    [Fact] void should_preserve_the_stored_string() => _value.Value.ShouldEqual("write  read write");
    [Fact] void should_compare_by_value() => _value.ShouldEqual(new ScopeSet("write  read write"));
    [Fact] void should_convert_to_the_primitive() => ((string)_value).ShouldEqual("write  read write");
    [Fact] void should_round_trip_the_value() => new ScopeSet(_value.Value).ShouldEqual(_value);
    [Fact] void should_split_without_empty_entries() => _value.Scopes.Select(_ => _.Value).ShouldEqual(new[] { "write", "read", "write" });
    [Fact] void should_sort_ordinally_without_removing_duplicates() => _canonical.Value.ShouldEqual("read write write");
    [Fact] void should_preserve_an_empty_value() => new ScopeSet(string.Empty).Value.ShouldEqual(string.Empty);
    [Fact] void should_split_an_empty_value_into_no_scopes() => new ScopeSet(string.Empty).Scopes.ShouldBeEmpty();
    [Fact] void should_join_an_empty_selection_as_an_empty_string() => ScopeSet.From([]).Value.ShouldEqual(string.Empty);
}
