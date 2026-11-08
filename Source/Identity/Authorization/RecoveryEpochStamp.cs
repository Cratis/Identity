// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Identity.Revocation;
using MongoDB.Bson;

namespace Cratis.Identity.Authorization;

/// <summary>Immutable operational continuity metadata, never a token claim.</summary>
internal static class RecoveryEpochStamp
{
    /// <summary>The operational property key, not a public token claim.</summary>
    internal const string PropertyName = "cratis_recovery_epoch";

    /// <summary>Stamps creation atomically with the operational insert.</summary>
    /// <param name="properties">The initial properties.</param>
    /// <param name="gate">The continuity guard.</param>
    /// <param name="restriction">The required restriction class.</param>
    /// <returns>The stamped property document.</returns>
    /// <exception cref="RevocationPending">The entry was already stamped or recovery is not covered.</exception>
    internal static BsonDocument Create(BsonDocument? properties, RecoveryGate gate, RestrictionClass restriction)
    {
        gate.Demand(restriction);
        var result = properties?.DeepClone().AsBsonDocument ?? new BsonDocument();
        if (result.Contains(PropertyName)) throw new RevocationPending();
        result[PropertyName] = gate.Epoch;
        return result;
    }

    /// <summary>Preserves the original stamp when stock managers replace properties.</summary>
    /// <param name="existing">The original properties.</param>
    /// <param name="replacement">The requested replacement.</param>
    /// <returns>The replacement with the unchanged stamp.</returns>
    /// <exception cref="RevocationPending">The replacement attempts to alter the immutable stamp.</exception>
    internal static ImmutableDictionary<string, JsonElement> Preserve(BsonDocument? existing, ImmutableDictionary<string, JsonElement> replacement)
    {
        if (existing?.TryGetValue(PropertyName, out var stamp) != true)
        {
            if (replacement.ContainsKey(PropertyName)) throw new RevocationPending();
            return replacement;
        }

        if (replacement.TryGetValue(PropertyName, out var proposed) &&
            (proposed.ValueKind != JsonValueKind.Number || !proposed.TryGetInt64(out var epoch) || !stamp.IsInt64 || epoch != stamp.AsInt64))
        {
            throw new RevocationPending();
        }

        return replacement.SetItem(PropertyName, JsonSerializer.SerializeToElement(stamp.AsInt64));
    }

    /// <summary>Retains the BSON representation through the stock JSON round-trip.</summary>
    /// <param name="existing">The original properties.</param>
    /// <param name="replacement">The replaced properties.</param>
    /// <returns>The replacement retaining the original stamp representation.</returns>
    internal static BsonDocument? Reapply(BsonDocument? existing, BsonDocument? replacement)
    {
        if (existing?.TryGetValue(PropertyName, out var stamp) == true)
        {
            replacement ??= new BsonDocument();
            replacement[PropertyName] = stamp;
        }

        return replacement;
    }

    /// <summary>Rejects direct updates that add, change or remove a persisted stamp.</summary>
    /// <param name="existing">The persisted properties.</param>
    /// <param name="replacement">The requested properties.</param>
    /// <exception cref="RevocationPending">The update changes the persisted stamp.</exception>
    internal static void DemandUnchanged(BsonDocument? existing, BsonDocument? replacement)
    {
        var before = existing?.GetValue(PropertyName, BsonNull.Value) ?? BsonNull.Value;
        var after = replacement?.GetValue(PropertyName, BsonNull.Value) ?? BsonNull.Value;
        if (!before.Equals(after)) throw new RevocationPending();
    }

    /// <summary>Accepts only the current epoch, with epoch-zero legacy compatibility.</summary>
    /// <param name="properties">The persisted properties.</param>
    /// <param name="epoch">The independently observed epoch.</param>
    /// <returns>Whether the entry belongs to the current epoch.</returns>
    internal static bool IsCurrent(ImmutableDictionary<string, JsonElement> properties, long epoch) =>
        properties.TryGetValue(PropertyName, out var stamp)
            ? stamp.ValueKind == JsonValueKind.Number && stamp.TryGetInt64(out var value) && value >= 0 && value == epoch
            : epoch == 0;
}
