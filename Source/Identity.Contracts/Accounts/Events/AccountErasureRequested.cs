// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>An accepted erasure request, not a retry signal. See the #26 identity-events-v1 catalogue.</summary>
/// <param name="ErasureId">The accepted request reference.</param>
/// <param name="RequestedAt">The acceptance time.</param>
[EventType]
public record AccountErasureRequested(ErasureId ErasureId, DateTimeOffset RequestedAt);
