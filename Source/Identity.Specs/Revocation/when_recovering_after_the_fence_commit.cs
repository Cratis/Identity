// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_recovering_after_the_fence_commit : Specification
{
    SharedFenceStore _store;
    IFenceLog _log;
    IRevocationFactWriter _writer;
    GrantFence _original;
    GrantState _state;

    async Task Establish()
    {
        _store = new SharedFenceStore();
        _original = await _store.Commit("grant", FenceDoubles.Subject, RevocationReason.RefreshTokenReused, CancellationToken.None);
        _log = Substitute.For<IFenceLog>();
        _log.Append(Arg.Any<GrantFence>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<GrantFence>().Sequence);
        _writer = Substitute.For<IRevocationFactWriter>();
    }

    async Task Because()
    {
        // Simulate restart with only durable state; no request-local objects or completed log append survive.
        await new DispatchRevocationOutbox(_store, new ConfirmFenceLog(_store, _log), _writer).Dispatch(CancellationToken.None);
        _state = await _store.Read("grant", CancellationToken.None);
    }

    [Fact] void should_resume_the_same_restriction() => _state.Fence.ShouldEqual(_original);
    [Fact]
    void should_confirm_the_log_before_any_source_fact() => Received.InOrder(() =>
    {
        _log.Append(_original, Arg.Any<CancellationToken>());
        _writer.Append(Arg.Is<OutboxEntry>(entry => entry.Kind == FactKind.AuthorizationRevocation), Arg.Any<CancellationToken>());
    });
    [Fact] void should_deliver_each_intent() => _state.Outbox.All(entry => entry.State == OutboxState.Delivered).ShouldBeTrue();
    [Fact] void should_preserve_the_persisted_reuse_identity() => _state.Outbox.Single(entry => entry.Kind == FactKind.Reuse).DetectionId.ShouldNotBeNull();
}
