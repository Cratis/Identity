// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Resources;

/// <summary>An opaque resource stream identity, never derived from an indicator or audience.</summary>
/// <param name="Value">The generated UUID.</param>
public record ResourceId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Allocates a new, independent stream identity.</summary>
    /// <returns>The identity.</returns>
    public static ResourceId New() => new(Guid.NewGuid());
}
