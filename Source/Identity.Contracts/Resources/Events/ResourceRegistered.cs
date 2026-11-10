// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Resources.Events;

/// <summary>A resource was registered; business labels remain Internal under amendment decision 4.</summary>
/// <param name="ResourceIndicator">The canonical exact-match absolute resource indicator.</param>
/// <param name="Audience">The issuer-wide unique audience.</param>
/// <param name="ScopePrefix">The resource's scope prefix.</param>
[EventType, Public]
public record ResourceRegistered(string ResourceIndicator, string Audience, string ScopePrefix);
