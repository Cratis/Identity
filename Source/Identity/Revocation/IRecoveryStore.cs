// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Restorable checkpoint and denial-only replay operations.</summary>
internal interface IRecoveryStore
{
    /// <summary>Reads operational recovery progress.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The checkpoint, if present.</returns>
    Task<RecoveryCheckpoint?> Checkpoint(CancellationToken cancellationToken);

    /// <summary>Checks for preexisting grant authority.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Whether operational grant state exists.</returns>
    Task<bool> HasState(CancellationToken cancellationToken);

    /// <summary>Verifies a retained denial and its immutable payload.</summary>
    /// <param name="record">The independently retained record.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Whether the denial is present.</returns>
    Task<bool> Contains(FenceRecord record, CancellationToken cancellationToken);

    /// <summary>Reestablishes a denial without creating admission authority.</summary>
    /// <param name="record">The independently retained record.</param>
    /// <param name="resetDelivery">Whether Chronicle delivery requires replay in this epoch.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Replay completion.</returns>
    Task Apply(FenceRecord record, bool resetDelivery, CancellationToken cancellationToken);

    /// <summary>Conditionally saves progress after all operational effects are proven.</summary>
    /// <param name="expected">The checkpoint observed before replay; a restore must not be overwritten.</param>
    /// <param name="checkpoint">The complete applied prefix and nonce.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Checkpoint completion.</returns>
    Task Save(RecoveryCheckpoint? expected, RecoveryCheckpoint checkpoint, CancellationToken cancellationToken);
}
