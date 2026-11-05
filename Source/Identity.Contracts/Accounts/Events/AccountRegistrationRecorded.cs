// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of AccountRegistered. See the #26 catalogue.</summary>
/// <param name="RegisteredAt">The registration time.</param>
[EventType("c2c8ac1b-6110-433f-93d6-1e9222852baa", generation: 1), Unique]
public record AccountRegistrationRecorded(DateTimeOffset RegisteredAt);
