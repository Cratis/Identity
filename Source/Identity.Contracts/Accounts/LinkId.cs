// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>Identifies a confirmed upstream link.</summary>
/// <param name="Value">The identifier.</param>
public record LinkId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Converts an identifier.</summary>
    /// <param name="value">The identifier.</param>
    public static implicit operator LinkId(Guid value) => new(value);
}
