// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_live_reservations_exist_during_steady_state : given.a_recovery_pass
{
    async Task Establish()
    {
        await Run();
        _allocated = 2;
        _log.Unresolved(Arg.Any<CancellationToken>()).Returns([new FenceReservation(Guid.NewGuid(), 2, "live-grant", ReservationState.Reserved, DateTimeOffset.UnixEpoch)]);
        _log.ClearReceivedCalls();
    }

    async Task Because() => await Run();

    [Fact] void should_not_cancel_a_live_writer_on_a_poll() => _authority.DidNotReceive().Cancel(Arg.Any<FenceReservation>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_treat_live_reservations_as_recovery_work() => _log.DidNotReceive().Unresolved(Arg.Any<CancellationToken>());
    [Fact] void should_keep_admission_open() => _gate.Ready.ShouldBeTrue();
    [Fact] void should_not_hold_the_delivery_worker() => _failure.ShouldBeNull();
}
