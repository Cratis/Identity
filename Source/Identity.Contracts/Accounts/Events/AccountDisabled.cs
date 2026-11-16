// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>The account was disabled behind a durable fence.</summary>
/// <param name="ReasonId">The reason reference.</param>
/// <param name="AccountEpoch">The new epoch.</param>
/// <param name="FenceId">The fence reference.</param>
/// <param name="FenceSequence">The durable fence sequence.</param>
[EventType, Public]
public record AccountDisabled(ReasonId ReasonId, AccountEpoch AccountEpoch, FenceId FenceId, FenceSequence FenceSequence);
