// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

/// <summary>Independent log fake: own sequence counter, tombstones, epoch and watermark, restorable on its own.</summary>
internal sealed class SnapshotableFenceLog : IIndependentFenceLog
{
    readonly object _gate = new();
    Dictionary<Guid, (long Sequence, string GrantId)> _reservations = [];
    Dictionary<long, FenceRecord> _records = [];
    Dictionary<string, long> _restores = [];
    long _next;
    long _epoch;

    internal FaultPoints Faults { get; } = new();

    internal sealed record Snapshotted(Dictionary<Guid, (long Sequence, string GrantId)> Reservations, Dictionary<long, FenceRecord> Records, Dictionary<string, long> Restores, long Next, long Epoch);

    internal Snapshotted Snapshot()
    {
        lock (_gate)
        {
            return new(new(_reservations), new(_records), new(_restores), _next, _epoch);
        }
    }

    internal void Restore(Snapshotted snapshot)
    {
        lock (_gate)
        {
            _reservations = new(snapshot.Reservations);
            _records = new(snapshot.Records);
            _restores = new(snapshot.Restores);
            _next = snapshot.Next;
            _epoch = snapshot.Epoch;
        }
    }

    public Task<long> Reserve(Guid fenceId, string grantId, CancellationToken cancellationToken)
    {
        long sequence;
        lock (_gate)
        {
            if (!_reservations.TryGetValue(fenceId, out var existing))
            {
                existing = (++_next, grantId);
                _reservations[fenceId] = existing;
            }

            sequence = existing.Sequence;
        }

        Faults.Reach(FaultPoint.AfterReserve);
        return Task.FromResult(sequence);
    }

    public Task<long> Append(FenceRecord record, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var prior = _records.Values.FirstOrDefault(existing => existing.Fence.GrantId == record.Fence.GrantId);
            if (prior is not null && prior.Fence.Id != record.Fence.Id)
            {
                throw new FenceLogConflict();
            }

            if (_records.TryGetValue(record.Fence.Sequence, out var same))
            {
                if (same.Fence != record.Fence || !same.IntentIds.SequenceEqual(record.IntentIds))
                {
                    throw new FenceLogConflict();
                }
            }
            else
            {
                _records[record.Fence.Sequence] = record;
            }
        }

        Faults.Reach(FaultPoint.AfterLogAppend);
        return Task.FromResult(record.Fence.Sequence);
    }

    public Task<long> Watermark(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var watermark = 0L;
            while (_records.ContainsKey(watermark + 1))
            {
                watermark++;
            }

            return Task.FromResult(watermark);
        }
    }

    public Task<IReadOnlyList<FenceRecord>> Read(long after, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<FenceRecord>>([.. _records.Where(pair => pair.Key > after).OrderBy(pair => pair.Key).Select(pair => pair.Value)]);
        }
    }

    public Task<FenceRecord?> Tombstone(string grantId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_records.Values.FirstOrDefault(record => record.Fence.GrantId == grantId));
        }
    }

    public Task<long> RecoveryEpoch(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_epoch);
        }
    }

    public Task<long> AdvanceEpoch(string restoreId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_restores.ContainsKey(restoreId))
            {
                _restores[restoreId] = ++_epoch;
            }

            return Task.FromResult(_epoch);
        }
    }
}
