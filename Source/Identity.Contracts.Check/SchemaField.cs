// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Check;

/// <summary>The persisted wire shape, reusing the content check's recursive metadata.</summary>
/// <param name="Name">The CLR member name.</param>
/// <param name="WireName">The serialized alias.</param>
/// <param name="Type">The exact CLR type.</param>
/// <param name="Pii">The effective PII protection.</param>
/// <param name="Nullable">Whether the member is optional.</param>
/// <param name="UnderlyingType">The concept's serialized primitive, if any.</param>
/// <param name="Fields">The nested serialized shape.</param>
internal sealed record SchemaField(
    string Name,
    string WireName,
    string Type,
    bool Pii,
    bool Nullable,
    string? UnderlyingType,
    IReadOnlyList<SchemaField> Fields)
{
    /// <summary>Projects existing metadata into a stable, ordered wire schema.</summary>
    /// <param name="members">The content check's member tree.</param>
    /// <returns>The canonical fields.</returns>
    internal static IReadOnlyList<SchemaField> From(IEnumerable<MemberShape> members) => members
        .OrderBy(member => member.ClrName, StringComparer.Ordinal)
        .Select(member => new SchemaField(member.ClrName, member.WireName, member.TypeName, member.Pii, member.Nullable, member.UnderlyingType, From(member.Members)))
        .ToArray();

    /// <summary>Canonicalizes framework type identities in memory, leaving all other metadata unchanged.</summary>
    /// <returns>The field with runtime-independent framework assembly identities.</returns>
    internal SchemaField Canonicalize() => this with
    {
        Type = FrameworkTypeIdentity.Canonicalize(Type),
        UnderlyingType = UnderlyingType is null ? null : FrameworkTypeIdentity.Canonicalize(UnderlyingType)
    };

    /// <summary>Compares recursive field metadata without depending on declaration order.</summary>
    /// <param name="left">The retained fields.</param>
    /// <param name="right">The proposed fields.</param>
    /// <returns>Whether both field sets have identical metadata.</returns>
    internal static bool Equivalent(IReadOnlyList<SchemaField> left, IReadOnlyList<SchemaField> right) =>
        left.Count == right.Count && left.OrderBy(field => field.Name, StringComparer.Ordinal)
            .Zip(right.OrderBy(field => field.Name, StringComparer.Ordinal))
            .All(pair => pair.First.Canonicalize() with { Fields = pair.Second.Fields } == pair.Second.Canonicalize() && Equivalent(pair.First.Fields, pair.Second.Fields));
}
