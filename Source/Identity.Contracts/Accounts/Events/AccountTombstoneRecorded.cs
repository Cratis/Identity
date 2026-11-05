// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of AccountTombstoned. See the #26 catalogue.</summary>
/// <param name="TombstonedAt">The tombstone time.</param>
/// <param name="FactId">The retry-stable fact identity; not present in the Public payload.</param>
[EventType("f67aa184-1052-4a3e-863a-13059de335e0", generation: 1)]
public record AccountTombstoneRecorded(DateTimeOffset TombstonedAt, FactId FactId);
