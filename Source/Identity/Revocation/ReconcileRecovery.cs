// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>One bounded, leased recovery pass. No failure path opens the host gate.</summary>
/// <param name="log">Independent custody.</param>
/// <param name="operational">Restorable grant recovery.</param>
/// <param name="authority">The operational cancellation authority.</param>
/// <param name="reservations">Reservation reconciliation.</param>
/// <param name="facts">Keyed Chronicle recovery publication.</param>
/// <param name="dispatch">Bounded restriction delivery.</param>
/// <param name="gate">The host admission barrier.</param>
/// <param name="session">The host incarnation and active declaration.</param>
/// <param name="options">Operator continuity evidence.</param>
/// <param name="timeProvider">The abandonment clock.</param>
/// <param name="logger">Recovery failure diagnostics.</param>
internal sealed class ReconcileRecovery(
    IRecoveryLog log,
    IRecoveryStore operational,
    IReservationAuthority authority,
    ReconcileReservation reservations,
    IRecoveryFactWriter facts,
    DispatchRevocationOutbox dispatch,
    RecoveryGate gate,
    RecoverySession session,
    FenceLogOptions options,
    TimeProvider timeProvider,
    ILogger<ReconcileRecovery>? logger = null)
{
    /// <summary>Reconciles both progress bounds and every retained permanent denial.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>One bounded recovery pass.</returns>
    internal async Task Run(CancellationToken cancellationToken)
    {
        try
        {
            await Reconcile(cancellationToken);
        }
        catch (Exception exception)
        {
            gate.Close();
            session.Reconciled = false;
            if (logger is not null)
            {
                RecoveryFailureDiagnostics.Warn(logger, exception);
            }
            throw;
        }
    }

    /// <summary>Retries durable epoch publications independently of ordinary operational admission.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Epoch intent publication completion.</returns>
    internal async Task PublishEpochs(CancellationToken cancellationToken)
    {
        await Bind(options.InstanceId, cancellationToken);
        foreach (var intent in await log.EpochIntents(cancellationToken))
        {
            await facts.Publish(intent, cancellationToken);
        }
    }

    /// <summary>Publishes C after proof of every intent in a complete contiguous prefix.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Complete-prefix confirmation.</returns>
    internal async Task ConfirmPublication(CancellationToken cancellationToken)
    {
        if (!await log.Acquire(session.Owner, cancellationToken))
        {
            return;
        }

        var watermark = await log.Watermark(cancellationToken);
        await PublishEpochs(cancellationToken);
        if (await dispatch.Pending(cancellationToken))
        {
            return;
        }

        await facts.Checkpoint(options.InstanceId, watermark, cancellationToken);
        await log.ConfirmPublication(session.Owner, watermark, cancellationToken);
    }

    static bool MatchesCheckpoint(RecoveryCheckpoint? checkpoint, RecoveryCustody custody) =>
        checkpoint is not null && !Diverged(checkpoint, custody) && custody.ClearedEpoch == custody.Epoch;

    static void RequireProgress(long watermark, long independent, long operational)
    {
        if (watermark < independent || watermark < operational)
        {
            throw new RevocationPending();
        }
    }

    static void ValidateIdentity(RecoveryCheckpoint? checkpoint, RecoveryCustody custody)
    {
        if (checkpoint is null)
        {
            return;
        }

        if (checkpoint.InstanceId != custody.InstanceId || checkpoint.AppliedThrough < 0)
        {
            throw new FenceLogConflict();
        }
    }

    static bool Diverged(RecoveryCheckpoint? checkpoint, RecoveryCustody custody) => checkpoint is not null &&
        (checkpoint.AppliedThrough != custody.AppliedThrough || checkpoint.Nonce != custody.Nonce);

    async Task Reconcile(CancellationToken cancellationToken)
    {
        var custody = await ReadCustody(cancellationToken);
        if (!await log.Acquire(session.Owner, cancellationToken))
        {
            await Follow(custody, cancellationToken);
            return;
        }

        // The previous owner may have advanced custody before the lease changed hands.
        custody = await ReadCustody(cancellationToken);
        var checkpoint = await operational.Checkpoint(cancellationToken);
        ValidateIdentity(checkpoint, custody);
        BeginRestore(await RolledBack(checkpoint, custody, cancellationToken));
        var recovering = !session.Reconciled || session.RestoreId is not null;
        if (recovering)
        {
            gate.Close();
            await RegisterRestore(cancellationToken);
        }

        var watermark = await ResolveReservations(recovering, cancellationToken);
        RequireProgress(watermark, custody.AppliedThrough, checkpoint?.AppliedThrough ?? 0);
        if (recovering)
        {
            await Replay(cancellationToken);
        }

        await SaveProgress(custody.InstanceId, watermark, checkpoint, cancellationToken);
        var final = await ReadCustody(cancellationToken);
        if (await RolledBack(await operational.Checkpoint(cancellationToken), final, cancellationToken))
        {
            throw new RevocationPending();
        }

        session.Reconciled = true;
        gate.Open(final.ClearedEpoch == final.Epoch);
        gate.MarkGrantsRecovered();
    }

    async Task<RecoveryCustody> ReadCustody(CancellationToken cancellationToken)
    {
        var custody = await log.Custody(cancellationToken);
        if (custody.InstanceId != options.InstanceId)
        {
            throw new FenceLogConflict();
        }

        if (custody.AppliedThrough < 0 || custody.Epoch < 0)
        {
            throw new RevocationPending();
        }

        gate.ObserveEpoch(custody.Epoch);
        return custody;
    }

    async Task<bool> RolledBack(RecoveryCheckpoint? checkpoint, RecoveryCustody custody, CancellationToken cancellationToken)
    {
        if (Diverged(checkpoint, custody))
        {
            return true;
        }

        if (await MissingCheckpoint(checkpoint, custody, cancellationToken))
        {
            return true;
        }

        var watermark = await log.Watermark(cancellationToken);
        RequireProgress(watermark, custody.AppliedThrough, checkpoint?.AppliedThrough ?? 0);
        foreach (var record in (await log.Read(0, cancellationToken)).Where(record => record.Fence.Sequence <= watermark))
        {
            if (!await operational.Contains(record, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    async Task<bool> MissingCheckpoint(RecoveryCheckpoint? checkpoint, RecoveryCustody custody, CancellationToken cancellationToken)
    {
        if (checkpoint is not null)
        {
            return false;
        }

        return custody.AppliedThrough > 0 || await operational.HasState(cancellationToken);
    }

    async Task RegisterRestore(CancellationToken cancellationToken)
    {
        if (session.RestoreId is null || session.Registered)
        {
            return;
        }

        gate.Close();
        gate.ObserveEpoch(await log.RegisterRestore(session.RestoreId, session.Kind, session.Owner, cancellationToken));
        session.Registered = true;
    }

    async Task Bind(string instanceId, CancellationToken cancellationToken)
    {
        if (!session.Bound)
        {
            await facts.Bind(instanceId, cancellationToken);
            session.Bound = true;
        }
    }

    async Task<long> ResolveReservations(bool recovering, CancellationToken cancellationToken)
    {
        // Poll abandoned work even after startup, without fencing live requests or
        // allowing a large backlog to monopolize a lease renewal pass.
        var abandonedBefore = timeProvider.GetUtcNow().AddSeconds(-30);
        foreach (var reservation in (await log.Unresolved(cancellationToken)).Take(100).Where(reservation => reservation.ReservedAt <= abandonedBefore))
        {
            gate.Close();
            await reservations.Reconcile(reservation, cancellationToken);
        }

        var watermark = await log.Watermark(cancellationToken);
        if (recovering && watermark != await log.AllocatedThrough(cancellationToken))
        {
            throw new RevocationPending();
        }

        return watermark;
    }

    async Task ReplayVoids(CancellationToken cancellationToken)
    {
        // Restore cancellation markers too: a delayed pre-restore writer must still lose.
        foreach (var reservation in await log.Voided(cancellationToken))
        {
            if (await authority.Cancel(reservation, cancellationToken) is not null)
            {
                throw new FenceLogConflict();
            }
        }
    }

    async Task Replay(CancellationToken cancellationToken)
    {
        await ReplayVoids(cancellationToken);
        foreach (var record in await log.Read(0, cancellationToken))
        {
            await operational.Apply(record, ChronicleRestored(), cancellationToken);
        }

        if (ChronicleRestored())
        {
            gate.Close();
            await Bind(options.InstanceId, cancellationToken);
            await dispatch.Dispatch(cancellationToken);
            if (await dispatch.Pending(cancellationToken))
            {
                throw new RevocationPending();
            }

            await PublishEpochs(cancellationToken);
        }
    }

    async Task SaveProgress(string instanceId, long watermark, RecoveryCheckpoint? expected, CancellationToken cancellationToken)
    {
        if (!await log.Acquire(session.Owner, cancellationToken))
        {
            throw new RevocationPending();
        }

        if (ChronicleRestored())
        {
            await facts.Checkpoint(instanceId, watermark, cancellationToken);
            await log.ConfirmPublication(session.Owner, watermark, cancellationToken);
        }

        // One nonce per lease owner, retained across polls. Publish independent evidence first;
        // never overwrite a checkpoint restored or advanced since this pass read it.
        var next = new RecoveryCheckpoint(instanceId, watermark, session.Owner);
        if (next != expected || Diverged(next, await ReadCustody(cancellationToken)))
        {
            await log.Checkpoint(session.Owner, next, cancellationToken);
            await operational.Save(expected, next, cancellationToken);
        }
        else if (await operational.Checkpoint(cancellationToken) != next)
        {
            throw new RevocationPending();
        }
        if (session.RestoreId is not null)
        {
            var cleared = options.RestrictionsReestablished && options.RestoreId == session.RestoreId;
            await log.CompleteRestore(session.Owner, session.RestoreId, cleared, cancellationToken);
            session.RestoreId = null;
            session.Registered = false;
            session.Kind = RestoreKind.None;
        }
    }

    async Task Follow(RecoveryCustody custody, CancellationToken cancellationToken)
    {
        if (options.RestoreKind != RestoreKind.None || session.RestoreId is not null)
        {
            gate.Close();
            return;
        }

        var checkpoint = await operational.Checkpoint(cancellationToken);
        ValidateIdentity(checkpoint, custody);
        if (!MatchesCheckpoint(checkpoint, custody) || await RolledBack(checkpoint, custody, cancellationToken))
        {
            gate.Close();
            return;
        }

        // Live reservations are not recovery gaps. An already-ready follower must not
        // close admission (or the delivery worker) whenever a request reserves a slot.
        if (!session.Reconciled && !await CompletePrefix(custody.AppliedThrough, cancellationToken))
        {
            return;
        }

        session.Reconciled = true;
        gate.Open(true);
        gate.MarkGrantsRecovered();
    }

    async Task<bool> CompletePrefix(long appliedThrough, CancellationToken cancellationToken) =>
        await log.Watermark(cancellationToken) == appliedThrough && await log.AllocatedThrough(cancellationToken) == appliedThrough;

    void BeginRestore(bool rollback)
    {
        if (session.RestoreId is not null)
        {
            return;
        }

        if (!session.DeclarationConsumed && options.RestoreKind != RestoreKind.None)
        {
            session.RestoreId = options.RestoreId;
            session.Kind = options.RestoreKind;
            session.DeclarationConsumed = true;
            if (ChronicleRestored())
            {
                session.Bound = false;
            }
        }
        else if (rollback)
        {
            session.RestoreId = $"detected-{Guid.NewGuid():N}";
            session.Kind = RestoreKind.Operational;
        }
    }

    bool ChronicleRestored() => session.Kind is RestoreKind.Chronicle or RestoreKind.Both;
}
