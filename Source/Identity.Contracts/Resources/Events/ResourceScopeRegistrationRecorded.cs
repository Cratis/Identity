// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Contracts.Resources.Events;

/// <summary>Internal source of ResourceScopeRegistered on the resource stream.</summary>
/// <param name="ScopeName">The exact scope name within the resource's prefix.</param>
/// <param name="Description">The non-person business description, Internal only.</param>
/// <param name="FactId">The retry-stable fact identity, Internal only.</param>
[EventType("c1635433-c34b-4b8f-b551-f20fd541fbde", generation: 1)]
public record ResourceScopeRegistrationRecorded(string ScopeName, ResourceScopeDescription Description, FactId FactId);
