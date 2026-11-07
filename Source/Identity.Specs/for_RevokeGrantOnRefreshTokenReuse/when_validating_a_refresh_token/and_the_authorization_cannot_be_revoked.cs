// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_RevokeGrantOnRefreshTokenReuse.when_validating_a_refresh_token;

public class and_the_authorization_cannot_be_revoked : given.a_refresh_token_being_validated
{
    Exception? _error;

    void Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(false);
        _tokens.HasStatusAsync(_token, Statuses.Redeemed, Arg.Any<CancellationToken>()).Returns(true);
        _authorizations.TryRevokeAsync(_authorization, Arg.Any<CancellationToken>()).Returns(false);
    }

    async Task Because() => _error = await Catch.Exception(async () => await _handler.HandleAsync(_context));

    [Fact] void should_fail_the_request() => _error.ShouldBeOfExactType<AuthorizationRevocationFailed>();
    [Fact] void should_not_revoke_the_tokens_without_the_authorization() => _tokens.DidNotReceive().RevokeByAuthorizationIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
