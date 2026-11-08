// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_recovery_owner_has_not_completed_replay : given.a_recovery_pass
{
    void Establish()
    {
        _log.Acquire(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _allocated = _watermark = 2;
    }

    async Task Because() => await Run();

    [Fact] void should_keep_the_other_replica_unready() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_not_write_a_competing_boot_nonce() => _operational.DidNotReceive().Save(Arg.Any<RecoveryCheckpoint?>(), Arg.Any<RecoveryCheckpoint>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_allocate_a_second_epoch() => _epochs.Count.ShouldEqual(0);
}
