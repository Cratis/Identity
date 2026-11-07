// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_operational_cancellation_is_uncertain : Specification
{
    IReservationLog _log;
    IReservationAuthority _authority;
    FenceReservation _reservation;
    Exception _failure;

    void Establish()
    {
        _log = Substitute.For<IReservationLog>();
        _authority = Substitute.For<IReservationAuthority>();
        _reservation = new(Guid.NewGuid(), 1, "grant", ReservationState.Reserved, DateTimeOffset.UnixEpoch);
        _log.Fence(_reservation, CancellationToken.None).Returns(ReservationState.Fencing);
        _authority.Cancel(_reservation, CancellationToken.None).Returns(Task.FromException<FenceRecord?>(new TimeoutException()));
    }

    async Task Because() => _failure = await Catch.Exception(() => new ReconcileReservation(_log, _authority).Reconcile(_reservation, CancellationToken.None));

    [Fact] void should_keep_the_failure_visible() => _failure.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_not_void_or_fill_the_uncertain_reservation() => _log.DidNotReceive().Resolve(Arg.Any<FenceReservation>(), Arg.Any<FenceRecord?>(), Arg.Any<CancellationToken>());
}
