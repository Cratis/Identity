// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_RevokeGrantOnRefreshTokenReuse.when_validating_a_refresh_token;

public class and_it_is_still_valid : given.a_refresh_token_being_validated
{
    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_not_reject_it() => _context.IsRejected.ShouldBeFalse();
    [Fact] void should_not_revoke_the_authorization() => _authorizations.DidNotReceive().TryRevokeAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_revoke_any_tokens() => _tokens.DidNotReceive().RevokeByAuthorizationIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
