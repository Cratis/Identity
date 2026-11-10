// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Clients;

/// <summary>The identifier of a client.</summary>
/// <param name="Value">The client identifier.</param>
internal sealed record ClientId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a primitive client identifier.</summary>
    /// <param name="value">The client identifier.</param>
    public static implicit operator ClientId(string value) => new(value);

    /// <summary>Preserves an optional client hint at the protocol boundary.</summary>
    /// <param name="value">The client hint, or null.</param>
    /// <returns>The identifier, or null when no hint was supplied.</returns>
    internal static ClientId? From(string? value) => value is null ? null : new(value);
}
