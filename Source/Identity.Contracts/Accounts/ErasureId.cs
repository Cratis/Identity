// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>An opaque reference to one accepted erasure request; carries no personal data.</summary>
/// <param name="Value">The reference.</param>
public record ErasureId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Converts a reference.</summary>
    /// <param name="value">The reference.</param>
    public static implicit operator ErasureId(Guid value) => new(value);

    /// <summary>Allocates a reference once, to be carried through retries of the request.</summary>
    /// <returns>The opaque reference.</returns>
    public static ErasureId New() => new(Guid.NewGuid());
}
