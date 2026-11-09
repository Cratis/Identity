// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Independent immutable epoch publication intent.</summary>
/// <param name="RestoreId">The unique declaration.</param>
/// <param name="Epoch">The independently allocated epoch.</param>
/// <param name="OccurredAt">The original occurrence time.</param>
/// <param name="FenceId">The stable recovery fence identity.</param>
/// <param name="FenceSequence">The independently allocated sequence.</param>
public sealed record RecoveryEpochIntent(string RestoreId, long Epoch, DateTimeOffset OccurredAt, Guid FenceId, long FenceSequence);
