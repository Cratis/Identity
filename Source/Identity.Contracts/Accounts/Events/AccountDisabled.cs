// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>The account was disabled behind a durable fence. See the #26 identity-events-v1 catalogue.</summary>
/// <param name="ReasonId">The reason reference.</param>
/// <param name="AccountEpoch">The new epoch.</param>
/// <param name="FenceId">The fence reference.</param>
/// <param name="FenceSequence">The durable fence sequence.</param>
[EventType("8e1e68fd-e4fb-456a-9af4-f95185be950b", generation: 1), Public]
public record AccountDisabled(ReasonId ReasonId, AccountEpoch AccountEpoch, FenceId FenceId, FenceSequence FenceSequence);
