// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;

namespace Cratis.Identity.for_EventSchemaCompatibility;

public class when_moving_released_ids_to_default_names : Specification
{
    const string LegacyId = "fd1cb0b5-17c1-4f34-b625-c318dc928d97";

    [Fact]
    public void should_accept_a_released_uuid_row_that_became_its_own_name() => Check(Legacy, Current).ShouldBeEmpty();

    [Fact]
    public void should_reject_a_changed_field_during_the_move() =>
        Check(Legacy, Current with { Catalogue = [Original with { Fields = [] }] }).ShouldContain(new Violation(LegacyPath, "released_schema_changed"));

    [Fact]
    public void should_reject_changed_fixture_bytes_during_the_move() =>
        Check(Legacy, Current with { Fixtures = new Dictionary<string, byte[]> { [Original.Fixture] = "changed"u8.ToArray() } })
            .ShouldContain(new Violation(Legacy.Catalogue[0].Fixture, "released_fixture_changed"));

    [Fact]
    public void should_reject_divergent_successor_bytes_even_when_the_old_uuid_path_is_retained()
    {
        var current = new ReleasedSchemaState([Original], new Dictionary<string, byte[]>
        {
            [Legacy.Catalogue[0].Fixture] = "{}"u8.ToArray(),
            [Original.Fixture] = "changed"u8.ToArray()
        });
        Check(Legacy, current).ShouldContain(new Violation(Legacy.Catalogue[0].Fixture, "released_fixture_changed"));
    }

    [Fact]
    public void should_reject_a_uuid_row_that_moved_to_a_different_name()
    {
        var other = Original with { Name = "OtherEvent", TypeId = "OtherEvent", Fixture = "fixtures/OtherEvent/1.json" };
        var current = new ReleasedSchemaState([other], new Dictionary<string, byte[]> { [other.Fixture] = "{}"u8.ToArray() });
        var violations = Check(Legacy, current);
        violations.ShouldContain(new Violation(LegacyPath, "released_schema_removed"));
        violations.ShouldContain(new Violation(Legacy.Catalogue[0].Fixture, "released_fixture_removed"));
    }

    [Fact]
    public void should_reject_a_uuid_row_that_moved_to_another_uuid()
    {
        var other = Original with { TypeId = "0e4bb9ea-86c7-4000-bbfd-308a2dbf4989", Fixture = "fixtures/0e4bb9ea-86c7-4000-bbfd-308a2dbf4989/1.json" };
        var current = new ReleasedSchemaState([other], new Dictionary<string, byte[]> { [other.Fixture] = "{}"u8.ToArray() });
        Check(Legacy, current).ShouldContain(new Violation(LegacyPath, "released_schema_removed"));
    }

    [Fact]
    public void should_reject_a_uuid_row_that_moved_to_a_name_in_another_namespace()
    {
        var other = Original with { Namespace = "Cratis.Identity.Other" };
        var current = new ReleasedSchemaState([other], new Dictionary<string, byte[]> { [other.Fixture] = "{}"u8.ToArray() });
        Check(Legacy, current).ShouldContain(new Violation(LegacyPath, "released_schema_removed"));
    }

    [Fact]
    public void should_reject_a_misplaced_fixture_for_the_moved_row()
    {
        var misplaced = Original with { Fixture = "fixtures/elsewhere.json" };
        var current = new ReleasedSchemaState([misplaced], new Dictionary<string, byte[]> { [misplaced.Fixture] = "{}"u8.ToArray() });
        Check(Legacy, current).ShouldContain(new Violation(LegacyPath, "released_schema_changed"));
    }

    [Fact]
    public void should_never_translate_an_already_name_based_released_row()
    {
        var renamed = Original with { TypeId = "RenamedEvent", Fixture = "fixtures/RenamedEvent/1.json" };
        var current = new ReleasedSchemaState([renamed], new Dictionary<string, byte[]> { [renamed.Fixture] = "{}"u8.ToArray() });
        var violations = ReleasedSchemaCompatibility.Check(Current, current);
        violations.ShouldContain(new Violation($"{Original.TypeId}/{Original.Generation}", "released_schema_removed"));
        violations.ShouldContain(new Violation(Original.Fixture, "released_fixture_removed"));
    }

    [Fact]
    public void should_never_translate_a_name_based_row_to_a_uuid()
    {
        var pinned = Original with { TypeId = LegacyId, Fixture = $"fixtures/{LegacyId}/1.json" };
        var current = new ReleasedSchemaState([pinned], new Dictionary<string, byte[]> { [pinned.Fixture] = "{}"u8.ToArray() });
        ReleasedSchemaCompatibility.Check(Current, current).ShouldContain(new Violation($"{Original.TypeId}/{Original.Generation}", "released_schema_removed"));
    }

    static EventSchema Original => SchemaCompatibility.Describe(typeof(AccountRegistered), ReflectionShapeBuilder.Build(typeof(AccountRegistered)));
    static ReleasedSchemaState Legacy
    {
        get
        {
            var row = Original with { TypeId = LegacyId, Fixture = $"fixtures/{LegacyId}/1.json" };
            return new([row], new Dictionary<string, byte[]> { [row.Fixture] = "{}"u8.ToArray() });
        }
    }

    static ReleasedSchemaState Current => new([Original], new Dictionary<string, byte[]> { [Original.Fixture] = "{}"u8.ToArray() });
    static string LegacyPath => $"{LegacyId}/{Original.Generation}";
    static IReadOnlyList<Violation> Check(ReleasedSchemaState released, ReleasedSchemaState current) => ReleasedSchemaCompatibility.Check(released, current);
}
