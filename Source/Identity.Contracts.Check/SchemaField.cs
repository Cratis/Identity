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
}
