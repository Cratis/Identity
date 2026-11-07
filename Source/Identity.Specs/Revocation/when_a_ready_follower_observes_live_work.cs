// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_ready_follower_observes_live_work : given.a_recovery_pass
{
    async Task Establish()
    {
        _log.Acquire(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        await Run();
        _allocated = 2;
        _session.Bound = false;
        _facts.Bind(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new RevocationPending()));
    }

    async Task Because() => await Run();

    [Fact] void should_not_close_the_follower_for_a_live_reservation() => _gate.Ready.ShouldBeTrue();
    [Fact] void should_not_require_chronicle_for_operational_admission() => _facts.DidNotReceive().Bind(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_interfere_with_the_live_writer() => _authority.DidNotReceive().Cancel(Arg.Any<FenceReservation>(), Arg.Any<CancellationToken>());
    [Fact] void should_finish_the_poll() => _failure.ShouldBeNull();
}
