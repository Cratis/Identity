// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>
/// A product asks Cratis Identity to retire a client permanently, on behalf of the person who asked. Cratis Identity acts on it only
/// when <paramref name="RequestedBy"/> is one of its security owners and the client was registered at runtime; a client Cratis Identity
/// was configured with cannot be retired this way. The outcome reaches the product as the <c language="text">ClientPermanentlyRetired</c> event.
/// </summary>
/// <param name="RegistrationId">The opaque registration id the product learned from the <c language="text">ClientAdded</c> event's stream.</param>
/// <param name="RequestedBy">The subject of the account that asked, as the product authenticated it.</param>
[EventType, Public]
public record ClientRetirementRequested(ClientRegistrationId RegistrationId, string RequestedBy);
