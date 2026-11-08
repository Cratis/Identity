// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_committed_steady_state_reservation_is_abandoned : given.a_steady_state_reservation
{
    FenceRecord _committed;

    void Establish()
    {
        _committed = _record with { Fence = _record.Fence with { Id = _reservation.Id, Sequence = _reservation.Sequence, GrantId = _reservation.GrantId } };
        _authority.Cancel(_reservation, Arg.Any<CancellationToken>()).Returns(_committed);
    }

    async Task Because() => await Run();

    [Fact] void should_fill_the_committed_reservation_instead_of_voiding() => _state.ShouldEqual(ReservationState.Filled);
    [Fact] void should_resolve_using_the_committed_record() => _log.Received(1).Resolve(_reservation, _committed, Arg.Any<CancellationToken>());
    [Fact] void should_advance_the_contiguous_prefix() => _checkpoint.AppliedThrough.ShouldEqual(3);
    [Fact] void should_keep_admission_open() => _gate.Ready.ShouldBeTrue();
    [Fact] void should_preserve_the_nonce() => _custody.Nonce.ShouldEqual(_nonce);
}
