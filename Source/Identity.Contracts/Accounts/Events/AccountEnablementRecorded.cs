// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of AccountEnabled. See the #26 catalogue.</summary>
/// <param name="AccountEpoch">The epoch.</param>
/// <param name="EnabledAt">The enable time.</param>
[EventType("46c54931-10f5-4069-9c22-b059008b3fc1", generation: 1)]
public record AccountEnablementRecorded(AccountEpoch AccountEpoch, DateTimeOffset EnabledAt);
