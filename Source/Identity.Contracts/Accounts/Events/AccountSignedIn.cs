// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>
/// The account completed a sign-in at this realm. Published for every successful sign-in, on the account's own stream, so a product can
/// keep a login history without reading Identity's store. A refused sign-in is not an event, and the contract carries no address,
/// device, token or personal value.
/// </summary>
/// <param name="SignedInAt">The server time the sign-in was accepted.</param>
/// <param name="ConnectionId">The upstream connection the account used.</param>
/// <param name="IdpIssuer">The exact upstream authority that proved the sign-in.</param>
[EventType, Public]
public record AccountSignedIn(DateTimeOffset SignedInAt, ConnectionId ConnectionId, UpstreamIssuer IdpIssuer);
