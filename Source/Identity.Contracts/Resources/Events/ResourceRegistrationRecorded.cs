// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Contracts.Resources.Events;

/// <summary>Internal source of ResourceRegistered; labels never enter the Public contract.</summary>
/// <param name="ResourceIndicator">The normalized HTTPS resource indicator.</param>
/// <param name="Audience">The permanently reserved audience.</param>
/// <param name="ScopePrefix">The resource's scope prefix.</param>
/// <param name="DisplayName">The non-person business label, Internal only.</param>
/// <param name="FactId">The retry-stable fact identity, Internal only.</param>
[EventType("9737db97-da6b-484e-afe6-760188549102", generation: 1), Unique]
public record ResourceRegistrationRecorded(string ResourceIndicator, string Audience, string ScopePrefix, ResourceDisplayName DisplayName, FactId FactId);
