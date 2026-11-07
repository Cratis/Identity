// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs.given;

public class a_steady_state_reservation : a_recovery_pass
{
    private protected FenceReservation _reservation;
    private protected ReservationState _state;
    protected string _nonce;
    protected FixedTimeProvider _time;

    async Task Establish()
    {
        _time = new(DateTimeOffset.UnixEpoch.AddDays(1));
        _recovery = new(_log, _operational, _authority, new ReconcileReservation(_log, _authority), _facts, _dispatch, _gate, _session, _options, _time);
        await Run();
        _nonce = _custody.Nonce;
        _allocated = 3;
        _reservation = new(Guid.NewGuid(), 2, "abandoned-grant", ReservationState.Reserved, _time.GetUtcNow());
        _state = ReservationState.Reserved;
        _log.Unresolved(Arg.Any<CancellationToken>()).Returns(_ => _state is ReservationState.Reserved or ReservationState.Fencing ? [_reservation] : []);
        _log.Fence(_reservation, Arg.Any<CancellationToken>()).Returns(_ => _state = ReservationState.Fencing);
        _authority.Cancel(_reservation, Arg.Any<CancellationToken>()).Returns((FenceRecord?)null);
        _log.Resolve(_reservation, Arg.Any<FenceRecord?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _state = call.Arg<FenceRecord?>() is null ? ReservationState.Voided : ReservationState.Filled;
            _watermark = _allocated;
            return Task.CompletedTask;
        });

        // The first steady-state poll sees a live writer and must leave it alone.
        await Run();
        _time.Now = _time.Now.AddSeconds(30);
        _log.ClearReceivedCalls();
        _authority.ClearReceivedCalls();
    }
}
