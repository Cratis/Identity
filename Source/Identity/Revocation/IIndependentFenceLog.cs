// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>The independent fence log custody: sequences, tombstones, watermark and recovery epoch; not yet wired into production.</summary>
internal interface IIndependentFenceLog
{
    /// <summary>Reserves a sequence for a fence identity; idempotent per fence identity.</summary>
    /// <param name="fenceId">The fence identity.</param>
    /// <param name="grantId">The authorization.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The reserved sequence.</returns>
    Task<long> Reserve(Guid fenceId, string grantId, CancellationToken cancellationToken);

    /// <summary>Appends once, or verifies identical prior content; conflicts must throw <see cref="FenceLogConflict"/>.</summary>
    /// <param name="record">The complete immutable record.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The original sequence.</returns>
    Task<long> Append(FenceRecord record, CancellationToken cancellationToken);

    /// <summary>Reads the highest contiguous filled sequence.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The watermark.</returns>
    Task<long> Watermark(CancellationToken cancellationToken);

    /// <summary>Reads records after a sequence, in order.</summary>
    /// <param name="after">The exclusive lower bound.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The records.</returns>
    Task<IReadOnlyList<FenceRecord>> Read(long after, CancellationToken cancellationToken);

    /// <summary>Reads the permanent tombstone for a grant.</summary>
    /// <param name="grantId">The authorization.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The record, or absence.</returns>
    Task<FenceRecord?> Tombstone(string grantId, CancellationToken cancellationToken);

    /// <summary>Reads the monotonic recovery epoch.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The epoch.</returns>
    Task<long> RecoveryEpoch(CancellationToken cancellationToken);

    /// <summary>Advances the epoch once per restore identity.</summary>
    /// <param name="restoreId">The restore identity.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The epoch after the call.</returns>
    Task<long> AdvanceEpoch(string restoreId, CancellationToken cancellationToken);
}
