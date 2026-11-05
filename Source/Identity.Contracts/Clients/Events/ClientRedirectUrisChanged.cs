// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>Internal accepted redirect-set change.</summary>
/// <param name="RedirectUris">Validated redirects, without signed or redeemable material.</param>
[EventType("772b4aba-8741-4c0b-8628-cae90a229afd", generation: 1)]
public record ClientRedirectUrisChanged(IEnumerable<RedirectUri> RedirectUris);
