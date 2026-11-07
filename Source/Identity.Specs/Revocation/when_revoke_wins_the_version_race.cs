// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_revoke_wins_the_version_race : Specification
{
    SharedFenceStore _store;
    RedeemOutcome _outcome;
    GrantState _state;

    void Establish() => _store = new SharedFenceStore();

    async Task Because()
    {
        var read = await _store.RecordFamilyStart("grant", "presented", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _store.BeforeRedeem = () =>
        {
            entered.SetResult();
            return release.Task;
        };
        var refresh = _store.RedeemAndInstall("grant", "presented", "successor", read.Version, CancellationToken.None);
        await entered.Task;
        await _store.Commit("grant", FenceDoubles.Subject, RevocationReason.Revoked, CancellationToken.None);
        release.SetResult();
        _outcome = await refresh;
        _state = await _store.Read("grant", CancellationToken.None);
    }

    [Fact] void should_reject_the_stale_refresh() => _outcome.ShouldEqual(RedeemOutcome.Fenced);
    [Fact] void should_not_install_any_successor() => _state.CurrentRefreshTokenId.ShouldBeNull();
    [Fact] void should_keep_the_tombstone() => _state.Fence.ShouldNotBeNull();
}
