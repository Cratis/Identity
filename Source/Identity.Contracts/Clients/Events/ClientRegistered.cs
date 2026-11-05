// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>Internal client registration fact.</summary>
/// <param name="ClientId">The validated OAuth identifier.</param>
/// <param name="RegistrationKind">The policy-approved origin.</param>
/// <param name="RegisteredAt">The registration time.</param>
[EventType("420e2bb2-039f-443a-9491-1a6323788add", generation: 1), Unique]
public record ClientRegistered(ClientId ClientId, RegistrationKind RegistrationKind, DateTimeOffset RegisteredAt);
