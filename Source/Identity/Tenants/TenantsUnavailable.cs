// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Tenants;

/// <summary>
/// Represents a failed lookup; no tenant selection may proceed.
/// </summary>
/// <param name="Reason">The fixed failure category, without response content.</param>
internal sealed record TenantsUnavailable(TenantsUnavailableReason Reason) : TenantLookup;
