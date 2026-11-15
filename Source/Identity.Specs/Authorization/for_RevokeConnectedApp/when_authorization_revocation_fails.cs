// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization.for_RevokeConnectedApp;

public class when_authorization_revocation_fails : given.a_connected_app
{
    void Establish() => _authorizations.TryRevokeAsync(_authorization, Arg.Any<CancellationToken>()).Returns(false);

    Task Because() => Revoke();

    [Fact] void should_not_acknowledge_success() => _error.ShouldBeOfExactType<AuthorizationRevocationFailed>();
    [Fact] void should_commit_the_fence_first() => _fences.Received(1).Commit("grant", Subject, RevocationReason.Revoked, Arg.Any<CancellationToken>());
    [Fact] void should_not_revoke_tokens() => _tokens.DidNotReceive().RevokeByAuthorizationIdAsync("grant", Arg.Any<CancellationToken>());
    [Fact] void should_not_confirm_the_log() => _fences.DidNotReceive().ConfirmLog(Arg.Any<GrantFence>(), Arg.Any<CancellationToken>());
}
