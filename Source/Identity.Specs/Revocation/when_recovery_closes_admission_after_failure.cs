// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_recovery_closes_admission_after_failure : given.a_recovery_pass
{
    void Establish()
    {
        // Admission was previously reconciled and open, so the failure must be what closes it.
        _gate.Open(true);
        _session.Reconciled = true;
        _log.Custody(Arg.Any<CancellationToken>()).Returns<RecoveryCustody>(_ => throw new RevocationPending());
    }

    async Task Because() => await Run();

    [Fact] void should_keep_admission_closed() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_reset_reconciled() => _session.Reconciled.ShouldBeFalse();
    [Fact] void should_preserve_the_failure() => _failure.ShouldBeOfExactType<RevocationPending>();
}
