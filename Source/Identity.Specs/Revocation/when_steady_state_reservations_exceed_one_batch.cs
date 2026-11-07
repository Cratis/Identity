// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_steady_state_reservations_exceed_one_batch : given.a_recovery_pass
{
    FenceReservation[] _reservations;

    async Task Establish()
    {
        await Run();
        _reservations = Enumerable.Range(2, 101).Select(sequence => new FenceReservation(Guid.NewGuid(), sequence, $"grant-{sequence}", ReservationState.Reserved, DateTimeOffset.UnixEpoch)).ToArray();
        _log.Unresolved(Arg.Any<CancellationToken>()).Returns(_reservations);
        _log.Fence(Arg.Any<FenceReservation>(), Arg.Any<CancellationToken>()).Returns(ReservationState.Fencing);
        _authority.Cancel(Arg.Any<FenceReservation>(), Arg.Any<CancellationToken>()).Returns((FenceRecord?)null);
        _allocated = 102;
        _log.ClearReceivedCalls();
        _authority.ClearReceivedCalls();
    }

    async Task Because() => await Run();

    [Fact] void should_bound_the_operational_handshakes_per_pass() => _authority.Received(100).Cancel(Arg.Any<FenceReservation>(), Arg.Any<CancellationToken>());
    [Fact] void should_leave_the_next_reservation_for_another_poll() => _log.DidNotReceive().Fence(_reservations[100], Arg.Any<CancellationToken>());
    [Fact] void should_finish_the_pass() => _failure.ShouldBeNull();
    [Fact] void should_not_advance_over_unresolved_work() => _checkpoint.AppliedThrough.ShouldEqual(1);
}
