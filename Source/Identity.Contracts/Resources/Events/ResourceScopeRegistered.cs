// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Resources.Events;

/// <summary>A resource scope was registered; business labels remain Internal under amendment decision 4.</summary>
/// <param name="ScopeName">The resource-local scope name.</param>
[EventType, Public]
public record ResourceScopeRegistered(string ScopeName);
