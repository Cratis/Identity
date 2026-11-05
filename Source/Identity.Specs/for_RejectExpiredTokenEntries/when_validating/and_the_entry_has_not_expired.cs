// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_RejectExpiredTokenEntries.when_validating;

public class and_the_entry_has_not_expired : given.a_token_to_validate
{
    ValidateTokenContext _context;

    void Establish() => Expires(KeyMaterial.Now.AddSeconds(1));

    async Task Because()
    {
        _context = Context();
        await _handler.HandleAsync(_context);
    }

    [Fact] void should_leave_it_untouched() => _context.IsRejected.ShouldBeFalse();
}
