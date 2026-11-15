// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization.for_RevokeConnectedApp;

public class when_revoking_an_owned_grant : given.a_connected_app
{
    Task Because() => Revoke();

    [Fact] void should_succeed() => _error.ShouldBeNull();
    [Fact] void should_fence_the_grant_and_all_families() => _fences.Received(1).Commit("grant", Subject, RevocationReason.Revoked, Arg.Any<CancellationToken>());
    [Fact] void should_revoke_all_authorization_tokens() => _tokens.Received(1).RevokeByAuthorizationIdAsync("grant", Arg.Any<CancellationToken>());
    [Fact] void should_confirm_the_independent_log() => _fences.Received(1).ConfirmLog(Arg.Any<GrantFence>(), Arg.Any<CancellationToken>());
}
