// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Identity.Revocation;
using MongoDB.Bson;

namespace Cratis.Identity.DPoP.Operational;

/// <summary>Immutable operational refresh-family sender binding; never a refresh-token claim.</summary>
internal static class DPoPBindingStamp
{
    /// <summary>The immutable refresh-entry property name.</summary>
    internal const string PropertyName = "cratis_dpop_jkt";

    /// <summary>Writes the admission's binding only at refresh entry creation.</summary>
    /// <param name="properties">The entry properties.</param>
    /// <param name="jkt">The request's verified key or null for an unbound grant.</param>
    /// <returns>The stamped properties.</returns>
    /// <exception cref="RevocationPending">A caller supplied a binding instead of admission.</exception>
    internal static BsonDocument Create(BsonDocument? properties, string? jkt)
    {
        var result = properties?.DeepClone().AsBsonDocument ?? new BsonDocument();
        if (result.Contains(PropertyName)) throw new RevocationPending();
        if (jkt is not null) result[PropertyName] = jkt;
        return result;
    }

    /// <summary>Preserves the immutable property across stock manager replacements.</summary>
    /// <param name="existing">The persisted properties.</param>
    /// <param name="replacement">The replacement.</param>
    /// <returns>The replacement retaining the binding.</returns>
    /// <exception cref="RevocationPending">The replacement changes or introduces a binding.</exception>
    internal static ImmutableDictionary<string, JsonElement> Preserve(BsonDocument? existing, ImmutableDictionary<string, JsonElement> replacement)
    {
        if (existing?.TryGetValue(PropertyName, out var stamp) != true)
        {
            if (replacement.ContainsKey(PropertyName)) throw new RevocationPending();
            return replacement;
        }

        if (!stamp.IsString) throw new RevocationPending();
        if (replacement.TryGetValue(PropertyName, out var proposed) && !SameBinding(proposed, stamp.AsString)) throw new RevocationPending();
        return replacement.SetItem(PropertyName, JsonSerializer.SerializeToElement(stamp.AsString));
    }

    /// <summary>Rejects updates that add, remove or replace a binding.</summary>
    /// <param name="existing">The persisted properties.</param>
    /// <param name="replacement">The replacement properties.</param>
    /// <exception cref="RevocationPending">The binding changed.</exception>
    internal static void DemandUnchanged(BsonDocument? existing, BsonDocument? replacement)
    {
        var before = existing?.GetValue(PropertyName, BsonNull.Value) ?? BsonNull.Value;
        var after = replacement?.GetValue(PropertyName, BsonNull.Value) ?? BsonNull.Value;
        if (!before.Equals(after)) throw new RevocationPending();
    }

    static bool SameBinding(JsonElement proposed, string existing) => proposed.ValueKind == JsonValueKind.String && proposed.GetString() == existing;
}
