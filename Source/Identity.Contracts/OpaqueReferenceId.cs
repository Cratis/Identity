// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts;

/// <summary>A non-redeemable UUID reference, such as a client-secret identity.</summary>
/// <param name="Value">The reference.</param>
public record OpaqueReferenceId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Creates a new opaque reference.</summary>
    /// <returns>A random UUID reference.</returns>
    public static OpaqueReferenceId New() => new(Guid.NewGuid());
}
