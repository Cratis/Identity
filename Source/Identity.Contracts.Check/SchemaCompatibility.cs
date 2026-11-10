// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Enforces the append-only history of persisted event generations.</summary>
internal static class SchemaCompatibility
{
    static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Gets the reviewed generation history embedded in this checker.</summary>
    internal static IReadOnlyList<EventSchema> Baseline { get; } = LoadBaseline();

    /// <summary>Projects a discovered artifact using the existing recursive shape reader.</summary>
    /// <param name="type">The discovered event artifact.</param>
    /// <param name="shape">The already inspected content shape.</param>
    /// <returns>The canonical persisted schema.</returns>
    internal static EventSchema Describe(Type type, EventShape shape) => new(
        type.Assembly.GetName().Name!,
        type.Namespace ?? string.Empty,
        shape.Name,
        shape.TypeId,
        shape.Generation,
        shape.Visibility,
        $"fixtures/{shape.TypeId}/{shape.Generation}.json",
        SchemaField.From(shape.Members));

    /// <summary>Checks retained declarations, schema immutability and fixture coverage.</summary>
    /// <param name="current">The compiled generation schemas.</param>
    /// <param name="baseline">The append-only reviewed schemas.</param>
    /// <param name="fixtureExists">Whether a generation has an embedded fixture.</param>
    /// <returns>Schema-only diagnostics, never fixture values.</returns>
    internal static IReadOnlyList<Violation> Check(IEnumerable<EventSchema> current, IEnumerable<EventSchema> baseline, Func<string, bool> fixtureExists)
    {
        var compiled = current.ToArray();
        var history = baseline.ToArray();
        var result = new List<Violation>();
        if (compiled.Length == 0 || history.Length == 0) result.Add(new("$", "schema_history_empty"));
        foreach (var duplicate in history.GroupBy(row => (row.TypeId, row.Generation)).Where(group => group.Count() > 1))
            result.Add(new($"{duplicate.Key.TypeId}/{duplicate.Key.Generation}", "schema_history_duplicate"));
        CheckRetained(compiled, history, fixtureExists, result);
        CheckRecorded(compiled, history, fixtureExists, result);
        CheckSequences(compiled, result);
        return result.Distinct().ToArray();
    }

    /// <summary>Opens a fixture without relying on the caller's working directory.</summary>
    /// <param name="path">The manifest's fixture path.</param>
    /// <returns>The fixture stream, or null when it is absent.</returns>
    internal static Stream? OpenFixture(string path) => typeof(SchemaCompatibility).Assembly.GetManifestResourceStream($"Cratis.Identity.Contracts.Check.{path.Replace('/', '.')}");

    /// <summary>Checks that the referenced resource exists.</summary>
    /// <param name="path">The fixture path.</param>
    /// <returns>Whether the resource exists.</returns>
    internal static bool FixtureExists(string path)
    {
        using var stream = OpenFixture(path);
        return stream is not null;
    }

    static EventSchema[] LoadBaseline()
    {
        using var stream = typeof(SchemaCompatibility).Assembly.GetManifestResourceStream("Cratis.Identity.Contracts.Check.catalogue.event-schemas.json")!;
        return JsonSerializer.Deserialize<EventSchema[]>(stream, _options)!;
    }

    static void CheckRetained(EventSchema[] compiled, EventSchema[] history, Func<string, bool> fixtureExists, List<Violation> result)
    {
        foreach (var retained in history)
        {
            var matches = compiled.Where(row => row.TypeId == retained.TypeId && row.Generation == retained.Generation).ToArray();
            var path = $"{retained.TypeId}/{retained.Generation}";
            if (!fixtureExists(retained.Fixture)) result.Add(new(path, "event_fixture_missing"));
            if (matches.Length == 0)
            {
                var changedIdentity = compiled.Any(row => SameArtifact(row, retained) && row.TypeId != retained.TypeId);
                result.Add(new(path, changedIdentity ? "event_type_id_changed" : "event_generation_removed"));
                continue;
            }
            foreach (var match in matches) Compare(retained, match, path, result);
        }
    }

    static bool SameArtifact(EventSchema left, EventSchema right) => left.Assembly == right.Assembly && left.Namespace == right.Namespace && left.Name == right.Name;

    static void CheckRecorded(EventSchema[] compiled, EventSchema[] history, Func<string, bool> fixtureExists, List<Violation> result)
    {
        foreach (var schema in compiled)
        {
            var path = $"{schema.TypeId}/{schema.Generation}";
            if (!history.Any(row => row.TypeId == schema.TypeId && row.Generation == schema.Generation))
                result.Add(new(path, "event_generation_not_recorded"));
            if (!fixtureExists(schema.Fixture)) result.Add(new(path, "event_fixture_missing"));
        }
    }

    static void CheckSequences(EventSchema[] compiled, List<Violation> result)
    {
        foreach (var group in compiled.GroupBy(row => row.TypeId))
        {
            var generations = group.Select(row => row.Generation).Distinct().Order().ToArray();
            if (generations.Where((generation, index) => generation != (uint)index + 1).Any())
                result.Add(new(group.Key, "event_generation_gap"));
        }
    }

    static void Compare(EventSchema retained, EventSchema compiled, string path, List<Violation> result)
    {
        if (retained.Assembly != compiled.Assembly) result.Add(new(path, "event_assembly_changed"));
        if (retained.Visibility != compiled.Visibility) result.Add(new(path, "event_visibility_changed"));
        CompareFields(retained.Fields, compiled.Fields, path, result);
    }

    static void CompareFields(IReadOnlyList<SchemaField> retained, IReadOnlyList<SchemaField> compiled, string path, List<Violation> result)
    {
        retained = retained.OrderBy(field => field.Name, StringComparer.Ordinal).ToArray();
        compiled = compiled.OrderBy(field => field.Name, StringComparer.Ordinal).ToArray();
        if (!retained.Select(field => field.Name).SequenceEqual(compiled.Select(field => field.Name), StringComparer.Ordinal))
        {
            result.Add(new(path, "event_fields_changed_without_generation"));
            return;
        }
        for (var index = 0; index < retained.Count; index++)
        {
            var before = retained[index].Canonicalize();
            var after = compiled[index].Canonicalize();
            var memberPath = $"{path}.{before.Name}";
            if (before with { Fields = after.Fields } != after) result.Add(new(memberPath, "event_field_changed_without_generation"));
            CompareFields(before.Fields, after.Fields, memberPath, result);
        }
    }
}
