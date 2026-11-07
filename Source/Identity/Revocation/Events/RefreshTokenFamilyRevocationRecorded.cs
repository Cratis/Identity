// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Revocation.Events;

/// <summary>Internal source; no entry identity or bearer value is included.</summary>
/// <param name="GrantId">The grant.</param>
/// <param name="ReasonId">The closed reason.</param>
/// <param name="FenceId">The permanent restriction.</param>
/// <param name="FenceSequence">The log sequence.</param>
/// <param name="FactId">The retry-stable fact.</param>
[EventType("a80e52ab-051e-4771-bff2-488dccd8aa44", generation: 1)]
internal sealed record RefreshTokenFamilyRevocationRecorded(GrantId GrantId, ReasonId ReasonId, FenceId FenceId, FenceSequence FenceSequence, FactId FactId);
