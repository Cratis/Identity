// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of AccountEnabled. See the #26 catalogue.</summary>
/// <param name="AccountEpoch">The epoch.</param>
/// <param name="EnabledAt">The enable time.</param>
/// <param name="FactId">The retry-stable fact identity; not present in the Public payload.</param>
[EventType]
public record AccountEnablementRecorded(AccountEpoch AccountEpoch, DateTimeOffset EnabledAt, FactId FactId);
