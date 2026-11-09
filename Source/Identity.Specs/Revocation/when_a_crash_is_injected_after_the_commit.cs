// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_crash_is_injected_after_the_commit : Specification
{
    SharedFenceStore _store;
    SnapshotableFenceLog _log;
    Exception _commitCrash;
    Exception _reserveCrash;
    GrantFence _retried;
    GrantState _state;

    async Task Establish()
    {
        _store = new SharedFenceStore();
        _log = new SnapshotableFenceLog();
        await _store.RecordFamilyStart("grant", "token", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
        _store.Faults.Arm(FaultPoint.AfterCommit);
        _log.Faults.Arm(FaultPoint.AfterReserve);
    }

    async Task Because()
    {
        _commitCrash = await Catch.Exception(() => _store.Commit("grant", FenceDoubles.Subject, RevocationReason.RefreshTokenReused, CancellationToken.None));
        _reserveCrash = await Catch.Exception(() => _log.Reserve(Guid.NewGuid(), "grant", CancellationToken.None));
        _retried = await _store.Commit("grant", FenceDoubles.Subject, RevocationReason.RefreshTokenReused, CancellationToken.None);
        _state = await _store.Read("grant", CancellationToken.None);
    }

    [Fact] void should_crash_at_the_armed_commit_point() => _commitCrash.ShouldBeOfExactType<InjectedFault>();
    [Fact] void should_crash_at_the_armed_reserve_point() => _reserveCrash.ShouldBeOfExactType<InjectedFault>();
    [Fact] void should_keep_the_durable_fence_after_the_crash() => _state.Fence.ShouldEqual(_retried);
    [Fact] void should_fire_each_armed_point_only_once() => _store.Faults.Reach(FaultPoint.AfterCommit);
}
