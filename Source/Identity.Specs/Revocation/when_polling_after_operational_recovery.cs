// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_polling_after_operational_recovery : given.a_recovery_pass
{
    async Task Because()
    {
        await Run();
        await Run();
        await Run();
    }

    [Fact] void should_keep_one_boot_nonce() => _checkpoint.Nonce.ShouldEqual(_session.Owner);
    [Fact] void should_not_rewrite_operational_progress() => _operational.Received(1).Save(Arg.Any<RecoveryCheckpoint?>(), Arg.Any<RecoveryCheckpoint>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_churn_independent_progress() => _log.Received(1).Checkpoint(_session.Owner, Arg.Any<RecoveryCheckpoint>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_detect_its_own_writes_as_a_restore() => _epochs.Count.ShouldEqual(0);
    [Fact] void should_remain_ready() => _gate.Ready.ShouldBeTrue();
}
