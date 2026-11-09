// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Check;

/// <summary>Protects reviewed history independently of changes to compiled event declarations.</summary>
internal static class ReleasedSchemaCompatibility
{
    /// <summary>Rejects any replacement or removal of a released row or fixture; additions are allowed.</summary>
    /// <param name="released">The history read from the requested trusted ref.</param>
    /// <param name="current">The proposed working-tree history.</param>
    /// <returns>Metadata-only diagnostics.</returns>
    internal static IReadOnlyList<Violation> Check(ReleasedSchemaState released, ReleasedSchemaState current)
    {
        var result = new List<Violation>();
        if (released.Catalogue.Count == 0 || released.Fixtures.Count == 0) result.Add(new("$", "released_schema_set_empty"));
        CheckRows(released.Catalogue, current.Catalogue, result);
        CheckFixtures(released.Fixtures, current.Fixtures, result);
        foreach (var row in released.Catalogue)
            if (!released.Fixtures.ContainsKey(row.Fixture)) result.Add(new($"{row.TypeId}/{row.Generation}", "released_fixture_missing"));
        return result;
    }

    static void CheckRows(IReadOnlyList<EventSchema> released, IReadOnlyList<EventSchema> current, List<Violation> result)
    {
        foreach (var row in released)
        {
            var path = $"{row.TypeId}/{row.Generation}";
            var matches = current.Where(candidate => candidate.TypeId == row.TypeId && candidate.Generation == row.Generation).ToArray();
            if (matches.Length == 0) result.Add(new(path, "released_schema_removed"));
            else if (matches.Length != 1 || !SameRow(row, matches[0])) result.Add(new(path, "released_schema_changed"));
        }
        foreach (var group in released.GroupBy(row => (row.TypeId, row.Generation)).Where(group => group.Count() != 1))
            result.Add(new($"{group.Key.TypeId}/{group.Key.Generation}", "released_schema_duplicate"));
    }

    static bool SameRow(EventSchema released, EventSchema current) =>
        released with { Fields = current.Fields } == current && SchemaField.Equivalent(released.Fields, current.Fields);

    static void CheckFixtures(IReadOnlyDictionary<string, byte[]> released, IReadOnlyDictionary<string, byte[]> current, List<Violation> result)
    {
        foreach (var fixture in released)
        {
            if (!current.TryGetValue(fixture.Key, out var bytes)) result.Add(new(fixture.Key, "released_fixture_removed"));
            else if (!fixture.Value.AsSpan().SequenceEqual(bytes)) result.Add(new(fixture.Key, "released_fixture_changed"));
        }
    }
}
