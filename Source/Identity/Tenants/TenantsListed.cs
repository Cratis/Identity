// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Tenants;

/// <summary>
/// Represents a successful lookup, including when no tenants are selectable.
/// </summary>
/// <param name="Tenants">The selectable tenants.</param>
public sealed record TenantsListed(IEnumerable<SelectableTenant> Tenants) : TenantLookup;
