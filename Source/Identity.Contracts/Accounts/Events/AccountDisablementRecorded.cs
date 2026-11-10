// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of AccountDisabled. See the #26 catalogue.</summary>
/// <param name="ReasonId">The reason reference.</param>
/// <param name="AccountEpoch">The new epoch.</param>
/// <param name="FenceId">The fence reference.</param>
/// <param name="FenceSequence">The durable sequence.</param>
/// <param name="FactId">The retry-stable fact identity; not present in the Public payload.</param>
[EventType]
public record AccountDisablementRecorded(ReasonId ReasonId, AccountEpoch AccountEpoch, FenceId FenceId, FenceSequence FenceSequence, FactId FactId);
