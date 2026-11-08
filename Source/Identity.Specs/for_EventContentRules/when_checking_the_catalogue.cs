// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;

namespace Cratis.Identity.for_EventContentRules;

public class when_checking_the_catalogue : Specification
{
    [Fact]
    public void should_mirror_every_section_eight_row()
    {
        var rows = TableRows("| AccountRegistered | fd1cb0b5-17c1-4f34-b625-c318dc928d97 |", "| RefreshTokenReuseDetected | bedbc6d5-2870-4cf7-aa5e-79e7a2bf3cb6 |");
        rows.Length.ShouldEqual(CatalogueChecker.Events.Count);
        var parsed = rows.Select(cells => new CatalogueRow(cells[0], Identity(cells[1]), cells[2], cells[3], cells[4], cells[5], cells[7].Contains("`Cratis.Identity.Revocation.Events`", StringComparison.Ordinal) ? "Cratis.Identity" : "Cratis.Identity.Contracts")).ToArray();
        parsed.ShouldEqual(CatalogueChecker.Events.ToArray());
    }

    [Fact]
    public void should_mirror_every_source_fact()
    {
        var rows = TableRows("| AccountRegistered | AccountRegistrationRecorded |", "| RealmRecoveryEpochAdvanced | RealmRecoveryEpochAdvanceRecorded |");
        rows.Select(cells => new SourceFactRow(cells[0], cells[1], Identity(cells[2]))).ToArray().ShouldEqual(CatalogueChecker.SourceFacts.ToArray());
    }

    [Fact]
    public void should_reject_an_unallocated_placeholder()
    {
        var row = CatalogueChecker.Rows.Single(row => row.Name == "PasskeyRemoved");
        CatalogueChecker.Check(Shape() with { Name = row.Name }, row).ShouldContain(new Violation("$", "event_id_not_allocated"));
    }

    [Fact]
    public void should_reject_duplicate_identities() => CatalogueChecker.CheckIdentities([Shape(), Shape()]).ShouldContain(new Violation("$", "duplicate_event_identity"));

    [Fact]
    public void should_reject_an_unknown_event() => CatalogueChecker.Check(Shape(), null).ShouldContain(new Violation("$", "event_not_catalogued"));

    [Fact]
    public void should_reject_missing_allocated_contracts()
    {
        using var output = new StringWriter();
        CheckRunner.Run([typeof(AccountRegistered)], output).ShouldEqual(1);
        output.ToString().Contains("allocated_event_missing", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void should_reject_visibility_changes() => Check(Shape() with { Visibility = "Internal" }).ShouldContain(new Violation("$", "event_visibility_mismatch"));

    [Fact]
    public void should_reject_identity_changes() => Check(Shape() with { TypeId = "not-an-explicit-id" }).ShouldContain(new Violation("$", "event_type_id_mismatch"));

    [Fact]
    public void should_reject_marker_changes() => Check(Shape() with { Marker = "Unmarked" }).ShouldContain(new Violation("$", "event_marker_mismatch"));

    [Fact]
    public void should_reject_generation_zero() => Check(Shape() with { Generation = 0 }).ShouldContain(new Violation("$", "event_marker_mismatch"));

    [Theory]
    [InlineData(1u, "EventType")]
    [InlineData(2u, "EventType")]
    [InlineData(1u, "EventTypeGenerationFor")]
    [InlineData(2u, "EventTypeGenerationFor")]
    public void should_accept_current_and_previous_generation_markers(uint generation, string marker) =>
        Check(Shape() with { Generation = generation, Marker = marker }).ShouldBeEmpty();

    [Fact]
    public void should_reject_field_changes() => Check(Shape() with { Members = [] }).ShouldContain(new Violation("$", "event_fields_mismatch"));

    [Fact]
    public void should_reject_optional_fields() => Check(Shape() with { Members = [Shape().Members[0] with { Nullable = true }] })
        .ShouldContain(new Violation("RegisteredAt", "nullable_event_field"));

    [Fact]
    public void should_reject_protection_mark_changes() => Check(Shape() with { Members = [Shape().Members[0] with { Pii = true }] })
        .ShouldContain(new Violation("RegisteredAt", "catalogue_pii_mismatch"));

    static EventShape Shape() => ReflectionShapeBuilder.Build(typeof(AccountRegistered));
    static IReadOnlyList<Violation> Check(EventShape shape) => CatalogueChecker.Check(shape, CatalogueChecker.Rows.Single(row => row.Name == "AccountRegistered"));
    static string? Identity(string value) => value == "assigned at implementation" ? null : value;

    static string[][] TableRows(string first, string last)
    {
        var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "identity-events-v1.md"));
        var start = Array.FindIndex(lines, line => line.StartsWith(first, StringComparison.Ordinal));
        var end = Array.FindIndex(lines, start, line => line.StartsWith(last, StringComparison.Ordinal));
        (start >= 0 && end >= start).ShouldBeTrue();
        return lines[start..(end + 1)].Select(line => line.Split('|', StringSplitOptions.TrimEntries)[1..^1]).ToArray();
    }
}
