// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>An allowlisted reason reference, never free-text evidence.</summary>
/// <param name="Value">The identifier.</param>
public record ReasonId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a reason reference.</summary>
    /// <param name="value">The reference.</param>
    public static implicit operator ReasonId(string value) => new(value);
}
