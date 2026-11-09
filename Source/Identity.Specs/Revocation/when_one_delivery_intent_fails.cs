// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_one_delivery_intent_fails : Specification
{
    Exception? _error;
    GrantState _state;

    async Task Because()
    {
        var store = new SharedFenceStore();
        await store.Commit("grant", FenceDoubles.Subject, RevocationReason.RefreshTokenReused, CancellationToken.None);
        var writer = Substitute.For<IRevocationFactWriter>();
        writer.Append(Arg.Is<OutboxEntry>(entry => entry.Kind == FactKind.AuthorizationRevocation), Arg.Any<CancellationToken>()).Returns(Task.FromException(new RevocationPending()));
        _error = await Catch.Exception(() => new DispatchRevocationOutbox(store, FenceDoubles.Confirmation(store), writer).Dispatch(CancellationToken.None));
        _state = await store.Read("grant", CancellationToken.None);
    }
    [Fact] void should_report_the_partial_failure() => _error.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_leave_the_failed_intent_retryable() => _state.Outbox.Single(entry => entry.Kind == FactKind.AuthorizationRevocation).State.ShouldEqual(OutboxState.LogConfirmed);
    [Fact] void should_deliver_the_other_independent_intents() => _state.Outbox.Where(entry => entry.Kind != FactKind.AuthorizationRevocation).All(entry => entry.State == OutboxState.Delivered).ShouldBeTrue();
}
