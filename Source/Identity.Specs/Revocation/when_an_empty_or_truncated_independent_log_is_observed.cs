// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_an_empty_or_truncated_independent_log_is_observed : given.a_recovery_pass
{
    void Establish()
    {
        _log.Read(0, Arg.Any<CancellationToken>()).Returns(_ => []);
        _allocated = _watermark = 0;
    }

    async Task Because() => await Run();

    [Fact] void should_refuse_readiness() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_report_the_continuity_failure() => _failure.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_reset_the_checkpoint_to_an_empty_log() => _operational.DidNotReceive().Save(Arg.Any<RecoveryCheckpoint?>(), Arg.Any<RecoveryCheckpoint>(), Arg.Any<CancellationToken>());
}
