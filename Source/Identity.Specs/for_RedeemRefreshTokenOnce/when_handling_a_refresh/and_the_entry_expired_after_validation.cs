// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_RedeemRefreshTokenOnce.when_handling_a_refresh;

public class and_the_entry_expired_after_validation : given.a_refresh_token_request
{
    void Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(true);
        _tokens.TryRedeemAsync(_token, Arg.Any<CancellationToken>()).Returns(true);
        _tokens.GetExpirationDateAsync(_token, Arg.Any<CancellationToken>()).Returns(KeyMaterial.Now);
    }

    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_reject_with_invalid_grant() => _context.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_not_redeem_the_token() => _tokens.DidNotReceive().TryRedeemAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_revoke_the_authorization_because_expiry_is_not_reuse() => _authorizations.DidNotReceive().TryRevokeAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
}
