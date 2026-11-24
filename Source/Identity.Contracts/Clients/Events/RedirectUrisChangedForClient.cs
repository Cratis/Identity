// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>The client's exact redirect allowlist was set or changed; never includes an authorization request URI.</summary>
/// <param name="RedirectUris">The exact allowed redirect URIs.</param>
[EventType, Public]
public record RedirectUrisChangedForClient(IEnumerable<string> RedirectUris);
