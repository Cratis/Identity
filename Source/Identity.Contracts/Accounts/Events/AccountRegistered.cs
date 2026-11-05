// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>An account subject was allocated. See the #26 identity-events-v1 catalogue.</summary>
/// <param name="RegisteredAt">The registration time.</param>
[EventType("fd1cb0b5-17c1-4f34-b625-c318dc928d97", generation: 1), Public]
public record AccountRegistered(DateTimeOffset RegisteredAt);
