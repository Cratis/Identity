// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_restore_races_progress_publication : given.a_recovery_pass
{
    readonly TaskCompletionSource _publishing = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    RecoveryCheckpoint? _backup;
    RecoveryCheckpoint? _afterRace;
    Exception? _raceFailure;
    bool _readyAfterRace;

    async Task Establish()
    {
        _backup = _checkpoint;
        await Run();
        _allocated = _watermark = 2;
        _log.Checkpoint(Arg.Any<string>(), Arg.Any<RecoveryCheckpoint>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var next = call.Arg<RecoveryCheckpoint>();
            _custody = _custody with { AppliedThrough = next.AppliedThrough, Nonce = next.Nonce };
            _publishing.TrySetResult();
            await _resume.Task.WaitAsync(TimeSpan.FromSeconds(5));
        });
    }

    async Task Because()
    {
        var pass = _recovery.Run(CancellationToken.None);
        await _publishing.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _checkpoint = _backup;
        _contains = false;
        _resume.SetResult();
        _raceFailure = await Catch.Exception(() => pass);
        _afterRace = _checkpoint;
        _readyAfterRace = _gate.Ready;
        await Run();
    }

    [Fact] void should_reject_the_stale_progress_write() => _raceFailure.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_mask_the_restored_checkpoint() => _afterRace.ShouldEqual(_backup);
    [Fact] void should_close_admission_on_the_race() => _readyAfterRace.ShouldBeFalse();
    [Fact] void should_detect_and_repair_the_restore_on_retry() => _epochs.Count.ShouldEqual(1);
    [Fact] void should_restore_the_missing_denial() => _contains.ShouldBeTrue();
    [Fact] void should_keep_uncovered_classes_denied() => _gate.Ready.ShouldBeFalse();
}
