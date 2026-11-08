// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_restoring_one_store_and_keeping_the_log : Specification
{
    SharedFenceStore _store;
    SnapshotableFenceLog _log;
    GrantState _afterRestore;
    FenceRecord _tombstone;
    long _watermark;
    long _epochAfterAdvances;

    async Task Establish()
    {
        _store = new SharedFenceStore();
        _log = new SnapshotableFenceLog();
        await _store.RecordFamilyStart("grant", "token", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
    }

    async Task Because()
    {
        var beforeRevoke = _store.Snapshot();
        var fence = await _store.Commit("grant", FenceDoubles.Subject, RevocationReason.RefreshTokenReused, CancellationToken.None);
        await _log.Reserve(fence.Id, "grant", CancellationToken.None);
        await _log.Append(new(fence, []), CancellationToken.None);

        _store.Restore(beforeRevoke);

        _afterRestore = await _store.Read("grant", CancellationToken.None);
        _tombstone = await _log.Tombstone("grant", CancellationToken.None);
        _watermark = await _log.Watermark(CancellationToken.None);
        await _log.AdvanceEpoch("restore-1", CancellationToken.None);
        _epochAfterAdvances = await _log.AdvanceEpoch("restore-1", CancellationToken.None);
    }

    [Fact] void should_lose_the_fence_in_the_operational_store() => _afterRestore.Fence.ShouldBeNull();
    [Fact] void should_keep_the_tombstone_in_the_log() => _tombstone.Fence.GrantId.ShouldEqual("grant");
    [Fact] void should_keep_the_log_watermark() => _watermark.ShouldEqual(1L);
    [Fact] void should_advance_the_epoch_once_per_restore_identity() => _epochAfterAdvances.ShouldEqual(1L);
}
