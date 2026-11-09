// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;

namespace Cratis.Identity.for_EventSchemaCompatibility;

public class when_checking_schema_history : Specification
{
    [Fact]
    public void should_accept_unchanged_history() => Check([Original], [Original]).ShouldBeEmpty();

    [Fact]
    public void should_reject_changed_fields_without_a_new_generation() =>
        Check([Original with { Fields = [] }], [Original]).ShouldContain(new Violation(Path, "event_fields_changed_without_generation"));

    [Theory]
    [InlineData("wire-name")]
    [InlineData("type")]
    [InlineData("pii")]
    [InlineData("nullable")]
    [InlineData("underlying-type")]
    public void should_reject_changes_to_persisted_field_metadata(string change)
    {
        var field = Original.Fields[0];
        var changed = change switch
        {
            "wire-name" => field with { WireName = "renamed" },
            "type" => field with { Type = "System.String" },
            "pii" => field with { Pii = true },
            "nullable" => field with { Nullable = true },
            _ => field with { UnderlyingType = "string" }
        };
        Check([Original with { Fields = [changed] }], [Original]).ShouldContain(new Violation($"{Path}.{field.Name}", "event_field_changed_without_generation"));
    }

    [Fact]
    public void should_reject_nested_shape_changes()
    {
        var nested = Original with { Fields = [Original.Fields[0] with { Fields = Original.Fields }] };
        Check([Original], [nested]).ShouldContain(new Violation($"{Path}.RegisteredAt", "event_fields_changed_without_generation"));
    }

    [Fact]
    public void should_reject_removed_events() => Check([], [Original]).ShouldContain(new Violation(Path, "event_generation_removed"));

    [Fact]
    public void should_reject_changed_stable_identities() => Check([Original with { TypeId = "changed" }], [Original]).ShouldContain(new Violation(Path, "event_type_id_changed"));

    [Fact]
    public void should_reject_changed_visibility() => Check([Original with { Visibility = "Internal" }], [Original]).ShouldContain(new Violation(Path, "event_visibility_changed"));

    [Fact]
    public void should_reject_generation_gaps()
    {
        var third = Original with { Generation = 3, Fixture = "third.json" };
        Check([Original, third], [Original, third]).ShouldContain(new Violation(Original.TypeId, "event_generation_gap"));
    }

    [Fact]
    public void should_reject_missing_previous_generations() =>
        Check([Next], [Original, Next]).ShouldContain(new Violation(Path, "event_generation_removed"));

    [Fact]
    public void should_reject_decreased_generations() =>
        Check([Original], [Original, Next]).ShouldContain(new Violation($"{Original.TypeId}/2", "event_generation_removed"));

    [Fact]
    public void should_reject_unrecorded_generations() =>
        Check([Original, Next], [Original]).ShouldContain(new Violation($"{Original.TypeId}/2", "event_generation_not_recorded"));

    [Fact]
    public void should_reject_missing_fixtures() =>
        SchemaCompatibility.Check([Original], [Original], _ => false).ShouldContain(new Violation(Path, "event_fixture_missing"));

    [Fact]
    public void should_accept_a_reviewed_new_generation_with_its_previous_record_and_fixtures() =>
        Check([Original with { Name = "PreviousAccountRegistered" }, Next], [Original, Next]).ShouldBeEmpty();

    [Fact]
    public void should_reject_empty_history() => Check([], []).ShouldContain(new Violation("$", "schema_history_empty"));

    [Fact]
    public void should_reject_duplicate_history() => Check([Original], [Original, Original]).ShouldContain(new Violation(Path, "schema_history_duplicate"));

    static EventSchema Original => SchemaCompatibility.Describe(typeof(AccountRegistered), ReflectionShapeBuilder.Build(typeof(AccountRegistered)));
    static EventSchema Next => Original with { Generation = 2, Fixture = "second.json", Fields = [] };
    static string Path => $"{Original.TypeId}/1";
    static IReadOnlyList<Violation> Check(EventSchema[] current, EventSchema[] baseline) => SchemaCompatibility.Check(current, baseline, _ => true);
}
