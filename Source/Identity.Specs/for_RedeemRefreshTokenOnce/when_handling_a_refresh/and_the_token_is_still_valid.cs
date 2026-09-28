// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_RedeemRefreshTokenOnce.when_handling_a_refresh;

public class and_the_token_is_still_valid : given.a_refresh_token_request
{
    void Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(true);
        _tokens.TryRedeemAsync(_token, Arg.Any<CancellationToken>()).Returns(true);
    }

    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_not_reject_the_request() => _context.IsRejected.ShouldBeFalse();
    [Fact] void should_redeem_the_token() => _tokens.Received(1).TryRedeemAsync(_token, Arg.Any<CancellationToken>());
    [Fact] void should_not_revoke_the_authorization() => _authorizations.DidNotReceive().TryRevokeAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
}
