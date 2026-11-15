// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

/// <summary>Shared atomic authority for protocol specs; gates surround, never replace, the atomic boundary.</summary>
internal sealed class SharedFenceStore : IFenceStore
{
    readonly object _gate = new();
    readonly Dictionary<string, GrantState> _states = [];
    long _sequence;

    internal Func<Task>? BeforeRedeem { get; set; }
    internal Func<Task>? BeforeRevoke { get; set; }
    internal bool FailDeliveryMark { get; set; }
    internal bool FailConfirmation { get; set; }
    internal FaultPoints Faults { get; } = new();

    internal sealed record Snapshotted(Dictionary<string, GrantState> States, long Sequence);

    internal Snapshotted Snapshot()
    {
        lock (_gate)
        {
            return new(new(_states), _sequence);
        }
    }

    internal void Restore(Snapshotted snapshot)
    {
        lock (_gate)
        {
            _states.Clear();
            foreach (var (grantId, state) in snapshot.States)
            {
                _states[grantId] = state;
            }

            _sequence = snapshot.Sequence;
        }
    }

    public Task<GrantState?> Read(string grantId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_states.GetValueOrDefault(grantId));
        }
    }

    public async Task<GrantFence> Commit(string grantId, string subject, RevocationReason reason, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var state = await Read(grantId, cancellationToken);
            if (state?.Fence is { } existing)
            {
                return existing;
            }

            if (BeforeRevoke is not null)
            {
                await BeforeRevoke();
            }

            lock (_gate)
            {
                if (_states.GetValueOrDefault(grantId)?.Version != state?.Version)
                {
                    continue;
                }

                var fence = new GrantFence(Guid.NewGuid(), ++_sequence, grantId, subject, reason, DateTimeOffset.UnixEpoch);
                List<OutboxEntry> entries = [.. state?.Outbox ?? [], OutboxEntry.Create(FactKind.AuthorizationRevocation, grantId, subject, fence.RecordedAt, fence: fence), OutboxEntry.Create(FactKind.FamilyRevocation, grantId, subject, fence.RecordedAt, fence: fence)];
                if (reason == RevocationReason.RefreshTokenReused)
                {
                    entries.Add(OutboxEntry.Create(FactKind.Reuse, grantId, subject, fence.RecordedAt, fence: fence));
                }

                _states[grantId] = new(grantId, (state?.Version ?? 0) + 1, null, fence, [.. entries]);
                Faults.Reach(FaultPoint.AfterCommit);
                return fence;
            }
        }

        throw new RevocationPending();
    }

    public Task<GrantState> RecordFamilyStart(string grantId, string tokenId, string subject, DateTimeOffset startedAt, DateTimeOffset absoluteExpiresAt, CancellationToken cancellationToken) =>
        RecordFamilyStart(new FamilyStart(grantId, tokenId, subject, startedAt, absoluteExpiresAt), cancellationToken);

    public Task<GrantState> RecordFamilyStart(FamilyStart start, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_states.TryGetValue(start.GrantId, out var state))
            {
                return Task.FromResult(state);
            }

            state = new(start.GrantId, 1, start.TokenId, null, [OutboxEntry.Create(FactKind.FamilyStarted, start.GrantId, start.Subject, start.StartedAt, start.AbsoluteExpiresAt)], 0, start.Link);
            _states[start.GrantId] = state;
            return Task.FromResult(state);
        }
    }

    public async Task<RedeemOutcome> RedeemAndInstall(string grantId, string consumedId, string successorId, long expectedVersion, CancellationToken cancellationToken)
    {
        if (BeforeRedeem is not null)
        {
            await BeforeRedeem();
        }

        lock (_gate)
        {
            if (!_states.TryGetValue(grantId, out var state))
            {
                return RedeemOutcome.Unknown;
            }

            if (state.Fence is not null)
            {
                return RedeemOutcome.Fenced;
            }

            if (state.CurrentRefreshTokenId != consumedId)
            {
                return RedeemOutcome.NotCurrent;
            }

            if (state.Version != expectedVersion)
            {
                return RedeemOutcome.VersionConflict;
            }

            _states[grantId] = state with { Version = state.Version + 1, CurrentRefreshTokenId = successorId };
            return RedeemOutcome.Installed;
        }
    }

    public Task<IReadOnlyList<GrantState>> Undelivered(int limit, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<GrantState>>(_states.Values.Where(state => state.Outbox.Any(entry => entry.State != OutboxState.Delivered)).Take(limit).ToArray());
        }
    }

    public Task ConfirmLog(GrantFence fence, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (FailConfirmation)
            {
                throw new RevocationPending();
            }

            var state = _states[fence.GrantId];
            if (state.Fence != fence)
            {
                throw new FenceLogConflict();
            }

            _states[fence.GrantId] = state with { Version = state.Version + 1, Outbox = state.Outbox.Select(entry => entry.State == OutboxState.Pending ? entry with { State = OutboxState.LogConfirmed } : entry).ToArray() };
            Faults.Reach(FaultPoint.AfterConfirm);
            return Task.CompletedTask;
        }
    }

    public Task Delivered(string grantId, string outboxId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (FailDeliveryMark)
            {
                throw new RevocationPending();
            }

            var state = _states[grantId];
            _states[grantId] = state with { Version = state.Version + 1, Outbox = state.Outbox.Select(entry => entry.Id == outboxId ? entry with { State = OutboxState.Delivered } : entry).ToArray() };
            Faults.Reach(FaultPoint.AfterDelivery);
            return Task.CompletedTask;
        }
    }
}
