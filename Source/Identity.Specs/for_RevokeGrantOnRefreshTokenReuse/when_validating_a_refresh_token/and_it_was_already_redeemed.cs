// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_RevokeGrantOnRefreshTokenReuse.when_validating_a_refresh_token;

public class and_it_was_already_redeemed : given.a_refresh_token_being_validated
{
    void Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(false);
        _tokens.HasStatusAsync(_token, Statuses.Redeemed, Arg.Any<CancellationToken>()).Returns(true);
    }

    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_reject_with_invalid_grant() => _context.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_revoke_the_authorization() => _authorizations.Received(1).TryRevokeAsync(_authorization, Arg.Any<CancellationToken>());
    [Fact] void should_revoke_the_tokens_of_the_authorization() => _tokens.Received(1).RevokeByAuthorizationIdAsync(AuthorizationId, Arg.Any<CancellationToken>());

    [Fact]
    void should_revoke_the_authorization_before_its_tokens() => Received.InOrder(() =>
    {
        _authorizations.TryRevokeAsync(_authorization, Arg.Any<CancellationToken>());
        _tokens.RevokeByAuthorizationIdAsync(AuthorizationId, Arg.Any<CancellationToken>());
    });
}
