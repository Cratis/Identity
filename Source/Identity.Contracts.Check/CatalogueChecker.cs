// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Checks compiled declarations against the reviewed event catalogue.</summary>
internal static class CatalogueChecker
{
    static CatalogueChecker()
    {
        using var stream = typeof(CatalogueChecker).Assembly.GetManifestResourceStream("Cratis.Identity.Contracts.Check.catalogue.identity-events-v1.json")!;
        using var json = JsonDocument.Parse(stream);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Events = json.RootElement.GetProperty("events").Deserialize<CatalogueRow[]>(options)!;
        SourceFacts = json.RootElement.GetProperty("sourceFacts").Deserialize<SourceFactRow[]>(options)!;
        Rows = Events.Concat(SourceFacts.Select(fact =>
        {
            var contract = Events.Single(row => row.Name == fact.Contract);
            return contract with { Name = fact.Name, TypeId = fact.TypeId, Visibility = "Internal", Fields = $"{contract.Fields}, FactId" };
        })).ToArray();
    }

    /// <summary>Gets section 8 rows, including not-yet-allocated placeholders.</summary>
    internal static IReadOnlyList<CatalogueRow> Events { get; }

    /// <summary>Gets section 4.1 source-fact mappings.</summary>
    internal static IReadOnlyList<SourceFactRow> SourceFacts { get; }

    /// <summary>Gets expanded contracts and source facts.</summary>
    internal static IReadOnlyList<CatalogueRow> Rows { get; }

    /// <summary>Resolves account-stream compliance subjects using the catalogue's exact CLR identity.</summary>
    /// <param name="type">The event artifact.</param>
    /// <returns>Whether the event uses the account stream subject.</returns>
    internal static bool HasAccountSubject(Type type) => Rows.Any(row => row.Name == type.Name && row.Subject == "Account" &&
        type.FullName == $"Cratis.Identity.Contracts.{row.Area}.Events.{row.Name}");

    /// <summary>Checks a declaration against its assigned catalogue row.</summary>
    /// <param name="shape">The compiled schema.</param>
    /// <param name="row">The assigned catalogue row.</param>
    /// <returns>The catalogue violations.</returns>
    internal static IReadOnlyList<Violation> Check(EventShape shape, CatalogueRow? row)
    {
        var result = new List<Violation>();
        if (row is null) return [new("$", "event_not_catalogued")];
        if (row.TypeId is null) result.Add(new("$", "event_id_not_allocated"));
        if (shape.TypeId != row.TypeId || !Guid.TryParse(shape.TypeId, out var id) || id == Guid.Empty || id.Version != 4)
            result.Add(new("$", "event_type_id_mismatch"));
        if (shape.Generation != 1 || shape.Marker != "EventType") result.Add(new("$", "event_marker_mismatch"));
        if (shape.Visibility != row.Visibility) result.Add(new("$", "event_visibility_mismatch"));
        var expected = row.Fields.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToDictionary(field => field.TrimEnd('†', '‡'), field => field.EndsWith('†') || field.EndsWith('‡'));
        if (!shape.Members.Select(member => member.ClrName).ToHashSet(StringComparer.Ordinal).SetEquals(expected.Keys))
            result.Add(new("$", "event_fields_mismatch"));
        foreach (var member in shape.Members)
        {
            if (member.Nullable) result.Add(new(member.ClrName, "nullable_event_field"));
            if (expected.TryGetValue(member.ClrName, out var pii) && member.Pii != pii) result.Add(new(member.ClrName, "catalogue_pii_mismatch"));
        }
        return result;
    }

    /// <summary>Rejects duplicate stable identities within a generation.</summary>
    /// <param name="shapes">The compiled schemas.</param>
    /// <returns>The duplicate identity violations.</returns>
    internal static IReadOnlyList<Violation> CheckIdentities(IEnumerable<EventShape> shapes) => shapes
        .GroupBy(shape => (shape.TypeId, shape.Generation)).Where(group => group.Count() > 1)
        .Select(_ => new Violation("$", "duplicate_event_identity")).ToArray();
}
