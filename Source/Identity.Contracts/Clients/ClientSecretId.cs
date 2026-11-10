// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Clients;

/// <summary>A non-redeemable reference to a secret revision, never the secret itself.</summary>
/// <param name="Value">The generated reference.</param>
public record ClientSecretId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Allocates a reference independently of secret material.</summary>
    /// <returns>The reference.</returns>
    public static ClientSecretId New() => new(Guid.NewGuid());
}
