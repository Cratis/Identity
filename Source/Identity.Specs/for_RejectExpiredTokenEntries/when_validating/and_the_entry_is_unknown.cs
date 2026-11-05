// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_RejectExpiredTokenEntries.when_validating;

public class and_the_entry_is_unknown : given.a_token_to_validate
{
    ValidateTokenContext _context;

    void Establish() => _tokens.FindByIdAsync(TokenId, Arg.Any<CancellationToken>()).Returns((object)null);

    async Task Because()
    {
        _context = Context();
        await _handler.HandleAsync(_context);
    }

    [Fact] void should_leave_it_to_openiddict() => _context.IsRejected.ShouldBeFalse();
    [Fact] void should_not_read_an_expiry() => _tokens.DidNotReceive().GetExpirationDateAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
}
