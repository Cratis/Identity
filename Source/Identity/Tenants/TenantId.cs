// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Tenants;

/// <summary>The identifier of a tenant.</summary>
/// <param name="Value">The tenant identifier.</param>
internal sealed record TenantId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a primitive tenant identifier.</summary>
    /// <param name="value">The tenant identifier.</param>
    public static implicit operator TenantId(string value) => new(value);
}
