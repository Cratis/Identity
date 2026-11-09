// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Identity.DPoP.Operational;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_RedeemRefreshTokenOnce.when_handling_a_refresh;

public class and_dpop_is_disabled_for_a_bound_family : given.a_refresh_token_request
{
    void Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(true);
        _tokens.GetPropertiesAsync(_token, Arg.Any<CancellationToken>()).Returns(ImmutableDictionary<string, JsonElement>.Empty.Add(DPoPBindingStamp.PropertyName, JsonSerializer.SerializeToElement("synthetic-key")));
    }

    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_not_downgrade_to_bearer() => _context.IsRejected.ShouldBeTrue();
    [Fact] void should_not_consume_the_entry() => _tokens.DidNotReceive().TryRedeemAsync(_token, Arg.Any<CancellationToken>());
    [Fact] void should_not_revoke_the_family() => _authorizations.DidNotReceive().TryRevokeAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_install_a_successor() => _issuance.SuccessorId.ShouldBeNull();
}
