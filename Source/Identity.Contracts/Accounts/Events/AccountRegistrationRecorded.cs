// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of AccountRegistered.</summary>
/// <param name="RegisteredAt">The registration time.</param>
/// <param name="FactId">The retry-stable fact identity; not present in the Public payload.</param>
[EventType, Unique]
public record AccountRegistrationRecorded(DateTimeOffset RegisteredAt, FactId FactId);
