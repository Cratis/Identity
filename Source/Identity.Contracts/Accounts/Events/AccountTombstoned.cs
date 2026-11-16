// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>The subject was permanently reserved.</summary>
/// <param name="TombstonedAt">The tombstone time.</param>
[EventType, Public]
public record AccountTombstoned(DateTimeOffset TombstonedAt);
