// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.ConsentFlow;

/// <summary>The server-generated identity and evidence idempotency key of a consent request.</summary>
/// <param name="Value">The request identity.</param>
internal record ConsentRequestId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Allocates a request identity.</summary>
    /// <returns>A new identity.</returns>
    public static ConsentRequestId New() => new(Guid.NewGuid());
}
