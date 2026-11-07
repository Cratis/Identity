// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_fresh_owner_has_no_chronicle_connection : given.a_recovery_pass
{
    Exception? _deliveryFailure;

    void Establish()
    {
        _session.Bound = false;
        _facts.Bind(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new RevocationPending()));
    }

    async Task Because()
    {
        await Run();
        _deliveryFailure = await Catch.Exception(() => _recovery.PublishEpochs(CancellationToken.None));
    }

    [Fact] void should_finish_operational_recovery() => _failure.ShouldBeNull();
    [Fact] void should_not_require_chronicle_before_ordinary_readiness() => _gate.Ready.ShouldBeTrue();
    [Fact] void should_still_hold_unavailable_chronicle_delivery() => _deliveryFailure.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_manufacture_a_delivery_checkpoint() => _facts.DidNotReceive().Checkpoint(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
}
