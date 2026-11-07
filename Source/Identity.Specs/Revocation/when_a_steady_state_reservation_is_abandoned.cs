// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_steady_state_reservation_is_abandoned : given.a_steady_state_reservation
{
    readonly TaskCompletionSource _cancelling = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<FenceRecord?> _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ReservationState _beforeCancellation;
    long _beforeWatermark;
    bool _beforeReady;

    void Establish()
    {
        _authority.Cancel(_reservation, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _cancelling.TrySetResult();
            return _cancelled.Task;
        });
    }

    async Task Because()
    {
        var pass = _recovery.Run(CancellationToken.None);
        await _cancelling.Task.WaitAsync(TimeSpan.FromSeconds(5), _time);
        _beforeCancellation = _state;
        _beforeWatermark = _watermark;
        _beforeReady = _gate.Ready;
        _cancelled.SetResult(null);
        await pass;
        await Run();
    }

    [Fact] void should_fence_before_waiting_for_operational_cancellation() => _beforeCancellation.ShouldEqual(ReservationState.Fencing);
    [Fact] void should_not_skip_the_hole_before_cancellation_wins() => _beforeWatermark.ShouldEqual(1);
    [Fact] void should_close_admission_while_cancellation_is_unproven() => _beforeReady.ShouldBeFalse();
    [Fact] void should_void_only_after_cancellation_wins() => _state.ShouldEqual(ReservationState.Voided);
    [Fact] void should_advance_the_contiguous_prefix_past_the_filled_tail() => _checkpoint.AppliedThrough.ShouldEqual(3);
    [Fact] void should_reopen_admission() => _gate.Ready.ShouldBeTrue();
    [Fact] void should_not_churn_the_nonce() => _custody.Nonce.ShouldEqual(_nonce);
    [Fact] void should_not_invent_a_restore_epoch() => _epochs.ShouldBeEmpty();
    [Fact] void should_cancel_only_once() => _authority.Received(1).Cancel(_reservation, Arg.Any<CancellationToken>());
    [Fact] void should_complete_the_next_poll() => _failure.ShouldBeNull();
}
