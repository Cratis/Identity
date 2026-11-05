// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_RejectExpiredTokenEntries.when_validating;

public class and_the_entry_expired_on_a_refresh_token : given.a_token_to_validate
{
    ValidateTokenContext _context;

    void Establish() => Expires(KeyMaterial.Now.AddHours(-1));

    async Task Because()
    {
        _context = Context();
        await _handler.HandleAsync(_context);
    }

    [Fact] void should_not_revoke_anything_because_expiry_is_not_reuse() => _tokens.DidNotReceiveWithAnyArgs().TryRevokeAsync(default!, TestContext.Current.CancellationToken);
    [Fact] void should_not_change_the_entry() => _tokens.DidNotReceiveWithAnyArgs().UpdateAsync(default!, TestContext.Current.CancellationToken);
}
