// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_recovery_is_blocked_by_a_live_gap : given.a_recovery_pass
{
    readonly TaskCompletionSource _delivering = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    OutboxEntry _entry;
    bool _closedDuringDelivery;

    void Establish()
    {
        _allocated = 2;
        var entries = _record.Intents.Select(entry => entry with { State = OutboxState.LogConfirmed }).ToArray();
        _entry = entries[0];
        var state = new GrantState("grant", 1, null, _record.Fence, entries);
        _fences.Undelivered(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([state]);
        _fences.Read("grant", Arg.Any<CancellationToken>()).Returns(state);
        _restrictionFacts.Append(_entry, Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            _delivering.SetResult();
            await _resume.Task.WaitAsync(TimeSpan.FromSeconds(5));
        });
    }

    async Task Because()
    {
        var pass = RevocationOutboxWorker.RecoverAndDeliver(_recovery, _dispatch, CancellationToken.None);
        await _delivering.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _closedDuringDelivery = !_gate.Ready;
        _resume.SetResult();
        _failure = await Catch.Exception(() => pass);
    }

    [Fact] void should_deliver_while_recovery_is_still_blocked() => _closedDuringDelivery.ShouldBeTrue();
    [Fact] void should_confirm_the_delivered_intent() => _fences.Received(1).Delivered("grant", _entry.Id, Arg.Any<CancellationToken>());
    [Fact] void should_preserve_the_pending_recovery_result() => _failure.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_publish_a_complete_prefix_for_failed_recovery() => _facts.DidNotReceive().Checkpoint(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
}
