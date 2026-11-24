// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of AccountSignedIn.</summary>
/// <param name="SignedInAt">The server time the sign-in was accepted.</param>
/// <param name="ConnectionId">The upstream connection the account used.</param>
/// <param name="IdpIssuer">The exact upstream authority that proved the sign-in.</param>
/// <param name="FactId">The retry-stable fact identity, derived from the accepted operation; not present in the Public payload.</param>
[EventType]
public record AccountSignInRecorded(DateTimeOffset SignedInAt, ConnectionId ConnectionId, UpstreamIssuer IdpIssuer, FactId FactId);
