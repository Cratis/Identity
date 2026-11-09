// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;

namespace Cratis.Identity.for_EventSchemaCompatibility;

public class when_checking_released_schema_history : Specification
{
    [Fact]
    public void should_accept_unchanged_released_history() => Check(Released).ShouldBeEmpty();

    [Fact]
    public void should_reject_a_changed_released_row() =>
        Check(Released with { Catalogue = [Original with { Fields = [] }] }).ShouldContain(new Violation(Path, "released_schema_changed"));

    [Theory]
    [InlineData("assembly")]
    [InlineData("namespace")]
    [InlineData("name")]
    [InlineData("visibility")]
    [InlineData("fixture")]
    public void should_protect_every_released_row_property(string property)
    {
        var changed = property switch
        {
            "assembly" => Original with { Assembly = "Changed" },
            "namespace" => Original with { Namespace = "Changed" },
            "name" => Original with { Name = "Changed" },
            "visibility" => Original with { Visibility = "Changed" },
            _ => Original with { Fixture = "changed.json" }
        };
        Check(Released with { Catalogue = [changed] }).ShouldContain(new Violation(Path, "released_schema_changed"));
    }

    [Fact]
    public void should_reject_a_removed_released_row() =>
        Check(Released with { Catalogue = [] }).ShouldContain(new Violation(Path, "released_schema_removed"));

    [Fact]
    public void should_reject_a_changed_released_fixture() =>
        Check(Released with { Fixtures = new Dictionary<string, byte[]> { [Original.Fixture] = "changed"u8.ToArray() } })
            .ShouldContain(new Violation(Original.Fixture, "released_fixture_changed"));

    [Fact]
    public void should_reject_a_removed_released_fixture() =>
        Check(Released with { Fixtures = new Dictionary<string, byte[]>() }).ShouldContain(new Violation(Original.Fixture, "released_fixture_removed"));

    [Fact]
    public void should_reject_deleting_the_entire_current_set()
    {
        var violations = Check(new([], new Dictionary<string, byte[]>()));
        violations.ShouldContain(new Violation(Path, "released_schema_removed"));
        violations.ShouldContain(new Violation(Original.Fixture, "released_fixture_removed"));
    }

    [Fact]
    public void should_check_fixtures_not_referenced_by_a_catalogue_row()
    {
        var released = Released with { Fixtures = new Dictionary<string, byte[]>(Released.Fixtures) { ["fixtures/unreferenced.json"] = "{}"u8.ToArray() } };
        ReleasedSchemaCompatibility.Check(released, Released).ShouldContain(new Violation("fixtures/unreferenced.json", "released_fixture_removed"));
    }

    [Fact]
    public void should_allow_added_rows_and_fixtures()
    {
        var next = Original with { Generation = 2, Fixture = "fixtures/next.json" };
        var current = Released with
        {
            Catalogue = [Original, next],
            Fixtures = new Dictionary<string, byte[]>(Released.Fixtures) { [next.Fixture] = "{}"u8.ToArray() }
        };
        Check(current).ShouldBeEmpty();
    }

    [Fact]
    public void should_ignore_recursive_field_order()
    {
        var first = Original.Fields[0];
        var second = first with { Name = "Second", WireName = "Second" };
        var retained = Original with { Fields = [first with { Fields = [first, second] }, second] };
        var reordered = retained with { Fields = [second, first with { Fields = [second, first] }] };
        ReleasedSchemaCompatibility.Check(Released with { Catalogue = [retained] }, Released with { Catalogue = [reordered] }).ShouldBeEmpty();
    }

    [Fact]
    public void should_fail_closed_for_an_empty_released_set_when_the_mode_is_requested() =>
        ReleasedSchemaCompatibility.Check(new([], new Dictionary<string, byte[]>()), Released).ShouldContain(new Violation("$", "released_schema_set_empty"));

    [Fact]
    public void should_fail_closed_for_an_empty_released_catalogue() =>
        ReleasedSchemaCompatibility.Check(Released with { Catalogue = [] }, Released).ShouldContain(new Violation("$", "released_schema_set_empty"));

    [Fact]
    public void should_fail_closed_for_an_empty_released_fixture_set() =>
        ReleasedSchemaCompatibility.Check(Released with { Fixtures = new Dictionary<string, byte[]>() }, Released).ShouldContain(new Violation("$", "released_schema_set_empty"));

    [Fact]
    public async Task should_fail_closed_for_an_unresolvable_requested_ref()
    {
        await using var output = new StringWriter();
        (await ReleasedSchemaCheck.Run("--not-a-ref", System.IO.Path.Combine(AppContext.BaseDirectory, "Identity.slnx"), output)).ShouldEqual(1);
        output.ToString().ShouldContain("released_ref_unavailable");
    }

    static EventSchema Original => SchemaCompatibility.Describe(typeof(AccountRegistered), ReflectionShapeBuilder.Build(typeof(AccountRegistered)));
    static ReleasedSchemaState Released => new([Original], new Dictionary<string, byte[]> { [Original.Fixture] = "{}"u8.ToArray() });
    static string Path => $"{Original.TypeId}/{Original.Generation}";
    static IReadOnlyList<Violation> Check(ReleasedSchemaState current) => ReleasedSchemaCompatibility.Check(Released, current);
}
