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
        CheckFixtures(released.Fixtures, current.Fixtures, LegacyFixturePaths(released.Catalogue, current.Catalogue), result);
        foreach (var row in released.Catalogue)
            if (!released.Fixtures.ContainsKey(row.Fixture)) result.Add(new($"{row.TypeId}/{row.Generation}", "released_fixture_missing"));
        return result;
    }

    static void CheckRows(IReadOnlyList<EventSchema> released, IReadOnlyList<EventSchema> current, List<Violation> result)
    {
        foreach (var row in released)
        {
            var (matches, legacy) = Matches(row, current);
            var path = $"{row.TypeId}/{row.Generation}";
            if (matches.Length == 0) result.Add(new(path, "released_schema_removed"));
            else if (matches.Length != 1 || !SameRow(row, matches[0], legacy)) result.Add(new(path, "released_schema_changed"));
        }
        foreach (var group in released.GroupBy(row => (row.TypeId, row.Generation)).Where(group => group.Count() != 1))
            result.Add(new($"{group.Key.TypeId}/{group.Key.Generation}", "released_schema_duplicate"));
    }

    /// <summary>A row released under a pinned UUID id may move once to Chronicle's default id, its own CLR name; the source is always the trusted released row and the destination its unchanged name.</summary>
    /// <param name="row">The released row.</param>
    /// <returns>True when the released id is a UUID.</returns>
    static bool IsLegacyId(EventSchema row) => Guid.TryParse(row.TypeId, out _);

    static bool IsDefaultNameSuccessor(EventSchema released, EventSchema candidate) =>
        candidate.Assembly == released.Assembly && candidate.Namespace == released.Namespace &&
        candidate.Name == released.Name && candidate.Generation == released.Generation && candidate.TypeId == released.Name;

    static (EventSchema[] Matches, bool Legacy) Matches(EventSchema row, IReadOnlyList<EventSchema> current)
    {
        var exact = current.Where(candidate => candidate.TypeId == row.TypeId && candidate.Generation == row.Generation).ToArray();
        if (exact.Length > 0 || !IsLegacyId(row)) return (exact, false);
        return (current.Where(candidate => IsDefaultNameSuccessor(row, candidate)).ToArray(), true);
    }

    static Dictionary<string, string> LegacyFixturePaths(IReadOnlyList<EventSchema> released, IReadOnlyList<EventSchema> current)
    {
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in released)
        {
            var (matches, legacy) = Matches(row, current);
            if (legacy && matches.Length == 1) paths[row.Fixture] = matches[0].Fixture;
        }
        return paths;
    }

    static bool SameRow(EventSchema released, EventSchema current, bool legacy) => legacy
        ? released with { Fields = current.Fields, TypeId = current.TypeId, Fixture = current.Fixture } == current &&
            current.Fixture == $"fixtures/{current.TypeId}/{current.Generation}.json" && SchemaField.Equivalent(released.Fields, current.Fields)
        : released with { Fields = current.Fields } == current && SchemaField.Equivalent(released.Fields, current.Fields);

    static void CheckFixtures(
        IReadOnlyDictionary<string, byte[]> released,
        IReadOnlyDictionary<string, byte[]> current,
        IReadOnlyDictionary<string, string> legacyPaths,
        List<Violation> result)
    {
        foreach (var fixture in released)
        {
            var path = legacyPaths.GetValueOrDefault(fixture.Key, fixture.Key);
            if (!current.TryGetValue(path, out var bytes)) result.Add(new(fixture.Key, "released_fixture_removed"));
            else if (!fixture.Value.AsSpan().SequenceEqual(bytes)) result.Add(new(fixture.Key, "released_fixture_changed"));
        }
    }
}
