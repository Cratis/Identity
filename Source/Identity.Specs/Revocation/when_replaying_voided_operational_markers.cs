// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_replaying_voided_operational_markers : given.a_recovery_pass
{
    FenceReservation _voided;

    void Establish()
    {
        _voided = new(Guid.NewGuid(), 2, "cancelled-grant", ReservationState.Voided, DateTimeOffset.UnixEpoch);
        _log.Voided(Arg.Any<CancellationToken>()).Returns(_ => [_voided]);
        _authority.Cancel(_voided, Arg.Any<CancellationToken>()).Returns((FenceRecord?)null);
        _watermark = _allocated = 2;
    }

    async Task Because() => await Run();

    [Fact] void should_reestablish_the_delayed_writer_cancellation() => _authority.Received(1).Cancel(_voided, Arg.Any<CancellationToken>());
    [Fact] void should_allow_the_contiguous_voided_prefix() => _checkpoint.AppliedThrough.ShouldEqual(2L);
    [Fact] void should_finish_without_failure() => _failure.ShouldBeNull();
}
