// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.ConsentFlow;

/// <summary>An opaque identity for one validated authorization request, carried only in its server-side or protected flow.</summary>
/// <param name="Value">The non-sensitive request correlation identity, not raw authorization parameters.</param>
public record AuthorizationRequestId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Allocates an identity when the server validates the original authorization request.</summary>
    /// <returns>The identity to bind into the protected flow.</returns>
    public static AuthorizationRequestId New() => new(Guid.NewGuid());
}
