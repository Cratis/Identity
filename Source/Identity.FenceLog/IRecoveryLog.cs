// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Independent restore custody and reservation reconciliation.</summary>
public interface IRecoveryLog : IIndependentFenceLog, IReservationLog
{
    /// <summary>Reads the allocated high-water mark, which is not the contiguous prefix.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The highest allocated sequence.</returns>
    Task<long> AllocatedThrough(CancellationToken cancellationToken);

    /// <summary>Reads independent continuity and coverage evidence.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The independent custody record.</returns>
    Task<RecoveryCustody> Custody(CancellationToken cancellationToken);

    /// <summary>Acquires or renews the single recovery owner's lease.</summary>
    /// <param name="owner">The host incarnation.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Whether this host owns recovery.</returns>
    Task<bool> Acquire(string owner, CancellationToken cancellationToken);

    /// <summary>Mirrors an applied checkpoint under the owner's unexpired lease.</summary>
    /// <param name="owner">The host incarnation.</param>
    /// <param name="checkpoint">The applied prefix and nonce.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Checkpoint completion.</returns>
    Task Checkpoint(string owner, RecoveryCheckpoint checkpoint, CancellationToken cancellationToken);

    /// <summary>Completes a unique declaration, recording independent restriction reconciliation.</summary>
    /// <param name="owner">The host incarnation.</param>
    /// <param name="restoreId">The unique declaration.</param>
    /// <param name="restrictionsReestablished">Whether the operator proved all uncovered restrictions.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Declaration completion.</returns>
    Task CompleteRestore(string owner, string restoreId, bool restrictionsReestablished, CancellationToken cancellationToken);

    /// <summary>Reads retained cancellation markers for operational replay.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Proven void reservations.</returns>
    Task<IReadOnlyList<FenceReservation>> Voided(CancellationToken cancellationToken);

    /// <summary>Reads immutable recovery epoch publication intents.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Original ordered intents.</returns>
    Task<IReadOnlyList<RecoveryEpochIntent>> EpochIntents(CancellationToken cancellationToken);

    /// <summary>Advances independent publication progress after complete Chronicle proof.</summary>
    /// <param name="owner">The host incarnation.</param>
    /// <param name="publishedThrough">The complete contiguous prefix.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Confirmation completion.</returns>
    Task ConfirmPublication(string owner, long publishedThrough, CancellationToken cancellationToken);

    /// <summary>Registers one restore declaration and its durable recovery fence atomically.</summary>
    /// <param name="restoreId">The unique operator declaration.</param>
    /// <param name="kind">The restored stores.</param>
    /// <param name="owner">The current lease holder and retry identity.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The independently held epoch.</returns>
    Task<long> RegisterRestore(string restoreId, RestoreKind kind, string owner, CancellationToken cancellationToken);
}
