// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>Identifies an authorized resolution.</summary>
/// <param name="Value">The reference.</param>
public record ResolutionId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Converts a reference.</summary>
    /// <param name="value">The reference.</param>
    public static implicit operator ResolutionId(Guid value) => new(value);
}
