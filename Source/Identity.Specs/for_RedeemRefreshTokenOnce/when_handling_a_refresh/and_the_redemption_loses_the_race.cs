// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_RedeemRefreshTokenOnce.when_handling_a_refresh;

public class and_the_redemption_loses_the_race : given.a_refresh_token_request
{
    void Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(true);
        _tokens.TryRedeemAsync(_token, Arg.Any<CancellationToken>()).Returns(false);
        _authorizations.TryRevokeAsync(_authorization, Arg.Any<CancellationToken>()).Returns(false, true);
    }

    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_reject_with_invalid_grant() => _context.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_retry_the_revocation_after_a_concurrency_loss() => _authorizations.Received(2).TryRevokeAsync(_authorization, Arg.Any<CancellationToken>());
    [Fact] void should_revoke_the_tokens_of_the_authorization() => _tokens.Received(1).RevokeByAuthorizationIdAsync(AuthorizationId, Arg.Any<CancellationToken>());
}
