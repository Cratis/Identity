// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_fenced_grant_is_refreshed : for_RedeemRefreshTokenOnce.given.a_refresh_token_request
{
    void Establish() => _fences.Read(AuthorizationId, Arg.Any<CancellationToken>()).Returns(new GrantState(AuthorizationId, 2, null, new GrantFence(Guid.NewGuid(), 1, AuthorizationId, "account", RevocationReason.Revoked, _time.GetUtcNow()), []));

    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_deny_without_the_dispatcher() => _context.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_not_redeem() => _tokens.DidNotReceive().TryRedeemAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
}
