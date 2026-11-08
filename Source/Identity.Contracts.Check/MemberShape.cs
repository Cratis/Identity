// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Check;

/// <summary>A value-free schema member including its wire alias.</summary>
/// <param name="ClrName">The CLR member name.</param>
/// <param name="WireName">The serialized member name.</param>
/// <param name="TypeName">The CLR type identity.</param>
/// <param name="Pii">Whether Chronicle resolves PII metadata.</param>
/// <param name="ForbiddenType">Whether the payload type is excluded.</param>
/// <param name="OpenShape">Whether the member can carry arbitrary content.</param>
/// <param name="Nullable">Whether the member is optional.</param>
/// <param name="Concept">The exact concept identity, when present.</param>
/// <param name="UnderlyingType">The serialized concept primitive.</param>
/// <param name="Members">The nested members.</param>
internal sealed record MemberShape(
    string ClrName,
    string WireName,
    string TypeName,
    bool Pii,
    bool ForbiddenType,
    bool OpenShape,
    bool Nullable,
    string? Concept,
    string? UnderlyingType,
    IReadOnlyList<MemberShape> Members);
