// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>Internal exact redirect allowlists changed; never includes an authorization request URI.</summary>
/// <param name="RedirectUris">The exact allowed redirect URIs.</param>
[EventType("1dfa8ff9-2538-44c2-8628-00dc084fcabe", generation: 1)]
public record ClientRedirectUrisChanged(IEnumerable<string> RedirectUris);
