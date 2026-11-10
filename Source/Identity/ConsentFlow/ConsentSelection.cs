// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.ConsentFlow;

/// <summary>The validated, immutable choice recorded with an approval.</summary>
/// <param name="Tenant">The eligible tenant chosen by the account.</param>
/// <param name="Scopes">The exact requested scope set in canonical order.</param>
internal record ConsentSelection(TenantId Tenant, ScopeSet Scopes);
