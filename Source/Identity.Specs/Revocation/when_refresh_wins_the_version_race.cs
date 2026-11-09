// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_refresh_wins_the_version_race : Specification
{
    SharedFenceStore _store;
    RedeemOutcome _installed;
    RedeemOutcome _successor;
    GrantState _state;

    void Establish() => _store = new SharedFenceStore();

    async Task Because()
    {
        var read = await _store.RecordFamilyStart("grant", "presented", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _store.BeforeRevoke = () =>
        {
            entered.TrySetResult();
            return release.Task;
        };
        var revoke = _store.Commit("grant", FenceDoubles.Subject, RevocationReason.Revoked, CancellationToken.None);
        await entered.Task;
        _installed = await _store.RedeemAndInstall("grant", "presented", "successor", read.Version, CancellationToken.None);
        release.SetResult();
        await revoke;
        _state = await _store.Read("grant", CancellationToken.None);
        _successor = await _store.RedeemAndInstall("grant", "successor", "later", _state.Version, CancellationToken.None);
    }

    [Fact] void should_install_exactly_the_preallocated_successor_first() => _installed.ShouldEqual(RedeemOutcome.Installed);
    [Fact] void should_deny_the_successors_next_use() => _successor.ShouldEqual(RedeemOutcome.Fenced);
    [Fact] void should_clear_the_admitted_entry_on_revoke() => _state.CurrentRefreshTokenId.ShouldBeNull();
    [Fact] void should_not_slide_or_duplicate_the_original_family() => _state.Outbox.Count(entry => entry.Kind == FactKind.FamilyStarted).ShouldEqual(1);
}
