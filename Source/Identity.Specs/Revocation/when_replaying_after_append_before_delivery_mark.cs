// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_replaying_after_append_before_delivery_mark : Specification
{
    SharedFenceStore _store;
    HashSet<string> _committed;
    List<string> _attempted;
    DispatchRevocationOutbox _first;
    ConfirmFenceLog _confirmation;
    IRevocationFactWriter _writer;
    Exception? _crash;
    GrantState _state;

    async Task Establish()
    {
        _store = new SharedFenceStore { FailDeliveryMark = true };
        await _store.Commit("grant", FenceDoubles.Subject, RevocationReason.RefreshTokenReused, CancellationToken.None);
        _committed = [];
        _attempted = [];
        _writer = Substitute.For<IRevocationFactWriter>();
        _writer.Append(Arg.Any<OutboxEntry>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var key = call.Arg<OutboxEntry>().Id;
            _attempted.Add(key);
            _committed.Add(key); // Models the kernel's durable keyed append result, including AlreadyAppended.
            return Task.CompletedTask;
        });
        _confirmation = FenceDoubles.Confirmation(_store);
        _first = new(_store, _confirmation, _writer);
    }

    async Task Because()
    {
        _crash = await Catch.Exception(() => _first.Dispatch(CancellationToken.None));
        _store.FailDeliveryMark = false;
        await new DispatchRevocationOutbox(_store, _confirmation, _writer).Dispatch(CancellationToken.None);
        _state = await _store.Read("grant", CancellationToken.None);
    }

    [Fact] void should_have_failed_after_a_committed_append() => _crash.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_retry_the_exact_same_delivery_key() => _attempted[0].ShouldEqual(_attempted[_state.Outbox.Length]);
    [Fact] void should_have_one_committed_fact_per_intent() => _committed.Count.ShouldEqual(_state.Outbox.Length);
    [Fact] void should_finish_all_delivery_marks() => _state.Outbox.All(entry => entry.State == OutboxState.Delivered).ShouldBeTrue();
}
