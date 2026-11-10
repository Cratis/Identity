// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>Internal client retirement.</summary>
/// <param name="ReasonId">The closed policy reason, never free-text credential material.</param>
/// <param name="RetiredAt">The retirement time.</param>
[EventType("8fa4f300-a66f-442d-ad15-37dc3bdd692f", generation: 1)]
public record ClientRetired(string ReasonId, DateTimeOffset RetiredAt);
