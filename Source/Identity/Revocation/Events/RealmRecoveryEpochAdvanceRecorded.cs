// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Revocation.Events;

/// <summary>Internal source for the realm recovery fence; Public publication remains behind its gate.</summary>
/// <param name="RestoreEpoch">The independently enforced epoch.</param>
/// <param name="ReasonId">The closed restore reason.</param>
/// <param name="FenceId">The stable recovery fence.</param>
/// <param name="FenceSequence">The independently allocated sequence.</param>
/// <param name="FactId">The retry-stable fact identity.</param>
[EventType]
internal sealed record RealmRecoveryEpochAdvanceRecorded(RestoreEpoch RestoreEpoch, ReasonId ReasonId, FenceId FenceId, FenceSequence FenceSequence, FactId FactId);
