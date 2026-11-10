// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Resources.Events;

/// <summary>A resource scope was retired.</summary>
/// <param name="ScopeName">The resource-local scope name.</param>
/// <param name="RetiredAt">The retirement time.</param>
[EventType, Public]
public record ResourceScopeRetired(string ScopeName, DateTimeOffset RetiredAt);
