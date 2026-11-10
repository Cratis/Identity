// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>The account was enabled without restoring grants. See the #26 identity-events-v1 catalogue.</summary>
/// <param name="AccountEpoch">The account epoch.</param>
/// <param name="EnabledAt">The enable time.</param>
[EventType, Public]
public record AccountEnabled(AccountEpoch AccountEpoch, DateTimeOffset EnabledAt);
