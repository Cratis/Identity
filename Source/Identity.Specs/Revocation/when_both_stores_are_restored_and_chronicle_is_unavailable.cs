// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_both_stores_are_restored_and_chronicle_is_unavailable : given.a_recovery_pass
{
    void Establish()
    {
        _contains = false;
        _checkpoint = _checkpoint with { AppliedThrough = 0, Nonce = "restored" };
        _options.RestoreKind = RestoreKind.Both;
        _options.RestoreId = "both-restore-test";
        _options.RestrictionsReestablished = true;
        _facts.Publish(Arg.Any<RecoveryEpochIntent>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new TimeoutException()));
    }

    async Task Because()
    {
        await Run();
        await Run();
    }

    [Fact] void should_replay_with_original_identities_on_every_retry() => _applied.TrueForAll(item => item.Record == _record && item.Reset).ShouldBeTrue();
    [Fact] void should_keep_the_epoch_stable_during_retry() => _epochs.Count.ShouldEqual(1);
    [Fact] void should_never_become_ready_after_retry_exhaustion() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_preserve_the_delivery_failure() => _failure.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_not_complete_the_declaration() => _log.DidNotReceive().CompleteRestore(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
}
