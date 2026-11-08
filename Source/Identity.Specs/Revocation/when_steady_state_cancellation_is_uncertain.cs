// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_steady_state_cancellation_is_uncertain : given.a_steady_state_reservation
{
    readonly TaskCompletionSource _cancelling = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<FenceRecord?> _outcome = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _authority.Cancel(_reservation, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _cancelling.TrySetResult();
            return _outcome.Task;
        });
    }

    async Task Because()
    {
        var pass = _recovery.Run(CancellationToken.None);
        await _cancelling.Task.WaitAsync(TimeSpan.FromSeconds(5), _time);
        _outcome.SetException(new TimeoutException());
        _failure = await Catch.Exception(() => pass);
    }

    [Fact] void should_surface_the_uncertain_outcome() => _failure.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_leave_the_reservation_unresolved() => _state.ShouldEqual(ReservationState.Fencing);
    [Fact] void should_not_void_or_fill() => _log.DidNotReceive().Resolve(Arg.Any<FenceReservation>(), Arg.Any<FenceRecord?>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_advance_the_contiguous_prefix() => _watermark.ShouldEqual(1);
    [Fact] void should_keep_admission_closed() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_preserve_the_checkpoint() => _checkpoint.AppliedThrough.ShouldEqual(1);
    [Fact] void should_preserve_the_nonce() => _custody.Nonce.ShouldEqual(_nonce);
}
