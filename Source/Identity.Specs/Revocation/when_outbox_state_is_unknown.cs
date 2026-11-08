// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs;

public class when_outbox_state_is_unknown : for_RedeemRefreshTokenOnce.given.a_refresh_token_request
{
    void Establish()
    {
        var entry = OutboxEntry.Create(FactKind.FamilyStarted, AuthorizationId, FenceDoubles.Subject, _time.GetUtcNow(), _time.GetUtcNow().AddDays(30)) with { State = (OutboxState)99 };
        _fences.Read(AuthorizationId, Arg.Any<CancellationToken>()).Returns(new GrantState(AuthorizationId, 1, TokenId, null, [entry]));
    }

    async Task Because() => await _handler.HandleAsync(_context);
    [Fact] void should_not_infer_permission_from_a_null_fence() => _context.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_not_update_the_entry() => _tokens.DidNotReceive().TryRedeemAsync(_token, Arg.Any<CancellationToken>());
    [Fact] void should_not_install_a_successor() => _issuance.SuccessorId.ShouldBeNull();
}
