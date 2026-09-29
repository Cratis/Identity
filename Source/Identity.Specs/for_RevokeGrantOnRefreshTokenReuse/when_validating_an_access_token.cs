// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_RevokeGrantOnRefreshTokenReuse;

public class when_validating_an_access_token : given.a_refresh_token_being_validated
{
    void Establish()
    {
        _context.Principal!.SetTokenType(TokenTypeIdentifiers.AccessToken);
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(false);
    }

    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_leave_it_to_openiddict() => _context.IsRejected.ShouldBeFalse();
    [Fact] void should_not_revoke_the_authorization() => _authorizations.DidNotReceive().TryRevokeAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
}
