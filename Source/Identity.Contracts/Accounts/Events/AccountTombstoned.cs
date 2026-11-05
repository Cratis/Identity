// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>The subject was permanently reserved. See the #26 identity-events-v1 catalogue.</summary>
/// <param name="TombstonedAt">The tombstone time.</param>
[EventType("6d6b4f99-5ac3-4493-b892-13a5e44c6518", generation: 1), Public]
public record AccountTombstoned(DateTimeOffset TombstonedAt);
