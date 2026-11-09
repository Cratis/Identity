// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Check;

/// <summary>An append-only persisted generation and its deserialization fixture.</summary>
/// <param name="Assembly">The production assembly name.</param>
/// <param name="Namespace">The declaring namespace.</param>
/// <param name="Name">The event name at allocation.</param>
/// <param name="TypeId">The stable event identity.</param>
/// <param name="Generation">The persisted generation.</param>
/// <param name="Visibility">The publication status.</param>
/// <param name="Fixture">The fixture resource path.</param>
/// <param name="Fields">The recursive serialized fields.</param>
internal sealed record EventSchema(
    string Assembly,
    string Namespace,
    string Name,
    string TypeId,
    uint Generation,
    string Visibility,
    string Fixture,
    IReadOnlyList<SchemaField> Fields);
