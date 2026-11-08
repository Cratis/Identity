// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>The version of the approved erasure completion boundary an erasure was completed against.</summary>
/// <param name="Value">The version.</param>
public record BoundaryVersion(int Value) : ConceptAs<int>(Value)
{
    /// <summary>Converts a version.</summary>
    /// <param name="value">The version.</param>
    public static implicit operator BoundaryVersion(int value) => new(value);
}
