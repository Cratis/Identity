// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Resources.Events;

/// <summary>A resource scope was retired.</summary>
/// <param name="ScopeName">The resource-local scope name.</param>
/// <param name="RetiredAt">The retirement time.</param>
[EventType("6a6869c9-423b-4746-8aa5-93b989b6895f", generation: 1), Public]
public record ResourceScopeRetired(string ScopeName, DateTimeOffset RetiredAt);
