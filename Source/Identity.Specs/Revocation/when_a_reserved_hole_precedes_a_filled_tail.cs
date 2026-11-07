// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_reserved_hole_precedes_a_filled_tail : given.a_recovery_pass
{
    void Establish()
    {
        _watermark = 1;
        _allocated = 3;
    }

    async Task Because() => await Run();

    [Fact] void should_keep_issuance_closed() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_report_unresolved_progress() => _failure.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_skip_the_hole() => _operational.DidNotReceive().Save(Arg.Any<RecoveryCheckpoint>(), Arg.Any<CancellationToken>());
}
