// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>An account subject was allocated. See the #26 identity-events-v1 catalogue.</summary>
/// <param name="RegisteredAt">The registration time.</param>
[EventType, Public]
public record AccountRegistered(DateTimeOffset RegisteredAt);
