// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Clients;

/// <summary>The opaque client event-source identity, deliberately not the OAuth client_id.</summary>
/// <param name="Value">The registration identity.</param>
public record ClientRegistrationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Creates a client event-source identity.</summary>
    /// <returns>A random UUID v4 identity.</returns>
    public static ClientRegistrationId New() => new(Guid.NewGuid());
}
