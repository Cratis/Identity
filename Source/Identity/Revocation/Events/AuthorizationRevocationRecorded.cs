// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Revocation.Events;

/// <summary>Internal source; Public publication remains behind the revocation gate.</summary>
/// <param name="AccountSubject">The account.</param>
/// <param name="ReasonId">The closed reason.</param>
/// <param name="GrantEpoch">The grant epoch.</param>
/// <param name="FenceId">The permanent restriction.</param>
/// <param name="FenceSequence">The log sequence.</param>
/// <param name="FactId">The retry-stable fact.</param>
[EventType]
internal sealed record AuthorizationRevocationRecorded(AccountSubject AccountSubject, ReasonId ReasonId, GrantEpoch GrantEpoch, FenceId FenceId, FenceSequence FenceSequence, FactId FactId);
