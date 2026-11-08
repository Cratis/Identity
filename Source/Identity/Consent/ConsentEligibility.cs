// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.ConsentFlow;

/// <summary>Validated catalogue inputs; never sufficient to activate a grant.</summary>
/// <param name="Grant">The exact requested grant.</param>
/// <param name="Tenants">The currently selectable tenants.</param>
/// <param name="ClientName">The current client display name, or null.</param>
internal record ConsentEligibility(Grant Grant, SelectableTenant[] Tenants, string? ClientName = null);
