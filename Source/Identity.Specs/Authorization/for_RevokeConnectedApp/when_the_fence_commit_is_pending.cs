// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization.for_RevokeConnectedApp;

public class when_the_fence_commit_is_pending : given.a_connected_app
{
    void Establish() => _fences.Commit("grant", Subject, RevocationReason.Revoked, Arg.Any<CancellationToken>())
        .Returns(Task.FromException<GrantFence>(new RevocationPending()));

    Task Because() => Revoke();

    [Fact] void should_not_acknowledge_success() => _error.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_revoke_the_authorization() => _authorizations.DidNotReceive().TryRevokeAsync(_authorization, Arg.Any<CancellationToken>());
    [Fact] void should_not_revoke_tokens() => _tokens.DidNotReceive().RevokeByAuthorizationIdAsync("grant", Arg.Any<CancellationToken>());
    [Fact] void should_not_confirm_the_log() => _fences.DidNotReceive().ConfirmLog(Arg.Any<GrantFence>(), Arg.Any<CancellationToken>());
}
