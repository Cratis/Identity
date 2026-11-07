// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.RevocationSpecs;

public class when_custody_advances_before_lease_acquisition : given.a_recovery_pass
{
    readonly TaskCompletionSource _acquiring = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _log.Acquire(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            _acquiring.TrySetResult();
            await _resume.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return true;
        });
    }

    async Task Because()
    {
        var pass = _recovery.Run(CancellationToken.None);
        await _acquiring.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _checkpoint = _checkpoint with { Nonce = "previous-owner-finished" };
        _custody = _custody with { Nonce = _checkpoint.Nonce };
        _resume.SetResult();
        _failure = await Catch.Exception(() => pass);
    }

    [Fact] void should_not_compare_progress_against_pre_lease_custody() => _epochs.Count.ShouldEqual(0);
    [Fact] void should_adopt_one_nonce_for_the_new_owner() => _checkpoint.Nonce.ShouldEqual(_session.Owner);
    [Fact] void should_admit_after_the_verified_handoff() => _gate.Ready.ShouldBeTrue();
    [Fact] void should_complete_without_a_spurious_restore() => _failure.ShouldBeNull();
}
