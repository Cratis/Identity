// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;

namespace Cratis.Identity.RevocationSpecs;

public class when_the_log_is_unavailable : Specification
{
    SharedFenceStore _store;
    GrantRevocation _protocol;
    IRevocationFactWriter _writer;
    Exception? _error;
    Exception? _dispatchError;
    GrantState _state;
    DispatchRevocationOutbox _dispatcher;

    async Task Establish()
    {
        _store = new SharedFenceStore();
        await _store.RecordFamilyStart("grant", "presented", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
        var log = Substitute.For<IFenceLog>();
        log.Append(Arg.Any<GrantFence>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<long>(new RevocationPending()));
        var confirm = new ConfirmFenceLog(_store, log);
        _protocol = new GrantRevocation(Substitute.For<IOpenIddictTokenManager>(), Substitute.For<IOpenIddictAuthorizationManager>(), _store, confirm);
        _writer = Substitute.For<IRevocationFactWriter>();
        _dispatcher = new(_store, confirm, _writer);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _protocol.RevokeAfterReuse("grant", CancellationToken.None));
        _dispatchError = await Catch.Exception(() => _dispatcher.Dispatch(CancellationToken.None));
        _state = await _store.Read("grant", CancellationToken.None);
    }

    [Fact] void should_not_acknowledge_the_request() => _error.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_keep_the_recovery_pass_pending() => _dispatchError.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_deny_even_while_both_chronicle_and_the_worker_are_unavailable() => _state.Fence.ShouldNotBeNull();
    [Fact] void should_not_publish_an_unconfirmed_restriction() => _writer.DidNotReceive().Append(Arg.Any<OutboxEntry>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_reopen_the_family() => _state.CurrentRefreshTokenId.ShouldBeNull();
}
