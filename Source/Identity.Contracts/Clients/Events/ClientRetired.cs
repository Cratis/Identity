// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>Internal client retirement.</summary>
/// <param name="ReasonId">The closed policy reason, never free-text credential material.</param>
/// <param name="RetiredAt">The retirement time.</param>
[EventType]
public record ClientRetired(string ReasonId, DateTimeOffset RetiredAt);
