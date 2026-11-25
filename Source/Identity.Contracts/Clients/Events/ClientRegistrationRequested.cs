// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>
/// A product asks Cratis Identity to register a public client, on behalf of the person who asked. The product appends it to its own
/// outbox; Cratis Identity acts on it only when <paramref name="RequestedBy"/> is one of its security owners. The client Identity
/// registers is a public native client using the authorization code flow with PKCE, and the outcome reaches the product as the
/// <c language="text">ClientAdded</c> event. A request for a client id that already exists changes nothing.
/// </summary>
/// <param name="ClientId">The protocol client id to reserve.</param>
/// <param name="RedirectUris">The exact loopback redirect URIs (<c language="text">127.0.0.1</c> or <c language="text">[::1]</c>, never <c language="text">localhost</c>).</param>
/// <param name="Scopes">The scopes the client may ask for. Each must be a scope Cratis Identity serves.</param>
/// <param name="RequestedBy">The subject of the account that asked, as the product authenticated it.</param>
[EventType, Public]
public record ClientRegistrationRequested(
    string ClientId,
    IEnumerable<string> RedirectUris,
    IEnumerable<string> Scopes,
    string RequestedBy);
