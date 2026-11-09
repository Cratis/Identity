// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Revocation.Events;

/// <summary>One immutable family lifecycle fact; rotation never emits another.</summary>
/// <param name="GrantId">The grant.</param>
/// <param name="AccountSubject">The account.</param>
/// <param name="StartedAt">The original admission time.</param>
/// <param name="AbsoluteExpiresAt">The original expiry, never sliding.</param>
/// <param name="FactId">The retry-stable fact.</param>
[EventType("d9a448be-ccf1-4ab5-b43c-f17dad0ba6c3", generation: 1)]
internal sealed record RefreshTokenFamilyStarted(GrantId GrantId, AccountSubject AccountSubject, DateTimeOffset StartedAt, DateTimeOffset AbsoluteExpiresAt, FactId FactId);
