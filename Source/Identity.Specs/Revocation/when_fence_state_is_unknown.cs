// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs;

public class when_fence_state_is_unknown : for_RedeemRefreshTokenOnce.given.a_refresh_token_request
{
    void Establish() => _fences.Read(AuthorizationId, Arg.Any<CancellationToken>()).Returns(Task.FromException<GrantState?>(new RevocationPending()));
    async Task Because() => await _handler.HandleAsync(_context);
    [Fact] void should_fail_closed() => _context.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_not_update_the_entry() => _tokens.DidNotReceive().TryRedeemAsync(_token, Arg.Any<CancellationToken>());
    [Fact] void should_not_install_a_successor() => _issuance.SuccessorId.ShouldBeNull();
}
