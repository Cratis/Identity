// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_committed_reservation_is_reconciled : given.a_recovery_pass
{
    FenceReservation _reservation;

    void Establish()
    {
        _reservation = new(_record.Fence.Id, 1, "grant", ReservationState.Reserved, DateTimeOffset.UnixEpoch);
        _log.Fence(_reservation, Arg.Any<CancellationToken>()).Returns(ReservationState.Fencing);
        _authority.Cancel(_reservation, Arg.Any<CancellationToken>()).Returns(_record);
    }

    async Task Because() => await new ReconcileReservation(_log, _authority).Reconcile(_reservation, CancellationToken.None);

    [Fact] void should_fill_the_original_record_instead_of_voiding() => _log.Received(1).Resolve(_reservation, _record, Arg.Any<CancellationToken>());
    [Fact] void should_never_discard_a_committed_denial() => _log.DidNotReceive().Resolve(_reservation, null, Arg.Any<CancellationToken>());
}
