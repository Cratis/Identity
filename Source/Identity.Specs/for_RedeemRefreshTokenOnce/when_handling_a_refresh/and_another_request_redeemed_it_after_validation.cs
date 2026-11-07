// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_RedeemRefreshTokenOnce.when_handling_a_refresh;

public class and_another_request_redeemed_it_after_validation : given.a_refresh_token_request
{
    void Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(false);
        _tokens.HasStatusAsync(_token, Statuses.Redeemed, Arg.Any<CancellationToken>()).Returns(true);
    }

    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_reject_with_invalid_grant() => _context.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_not_redeem_the_token_again() => _tokens.DidNotReceive().TryRedeemAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    [Fact] void should_revoke_the_authorization() => _authorizations.Received(1).TryRevokeAsync(_authorization, Arg.Any<CancellationToken>());
    [Fact] void should_revoke_the_tokens_of_the_authorization() => _tokens.Received(1).RevokeByAuthorizationIdAsync(AuthorizationId, Arg.Any<CancellationToken>());
}
