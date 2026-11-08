// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_two_refreshes_compete_for_one_admission : Specification
{
    RedeemOutcome[] _outcomes;
    GrantState _state;

    async Task Because()
    {
        var store = new SharedFenceStore();
        var read = await store.RecordFamilyStart("grant", "presented", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
        var arrived = 0;
        var both = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store.BeforeRedeem = () =>
        {
            if (Interlocked.Increment(ref arrived) == 2)
            {
                both.SetResult();
            }

            return release.Task;
        };
        var first = store.RedeemAndInstall("grant", "presented", "first", read.Version, CancellationToken.None);
        var second = store.RedeemAndInstall("grant", "presented", "second", read.Version, CancellationToken.None);
        await both.Task;
        release.SetResult();
        _outcomes = await Task.WhenAll(first, second);
        _state = await store.Read("grant", CancellationToken.None);
    }
    [Fact] void should_install_one_successor_only() => _outcomes.Count(outcome => outcome == RedeemOutcome.Installed).ShouldEqual(1);
    [Fact] void should_report_consumption_to_the_other_request() => _outcomes.Count(outcome => outcome == RedeemOutcome.NotCurrent).ShouldEqual(1);
    [Fact] void should_not_keep_the_presented_entry_current() => (string.Equals(_state.CurrentRefreshTokenId, "first", StringComparison.Ordinal) || string.Equals(_state.CurrentRefreshTokenId, "second", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_emit_rotation_facts() => _state.Outbox.Length.ShouldEqual(1);
}
