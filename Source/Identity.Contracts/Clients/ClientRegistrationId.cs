// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Clients;

/// <summary>An opaque client stream identity, never derived from the protocol client id.</summary>
/// <param name="Value">The generated UUID.</param>
public record ClientRegistrationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Allocates a new, independent stream identity.</summary>
    /// <returns>The identity.</returns>
    public static ClientRegistrationId New() => new(Guid.NewGuid());
}
