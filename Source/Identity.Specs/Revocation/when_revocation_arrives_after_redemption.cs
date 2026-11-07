// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs;

public class when_revocation_arrives_after_redemption : for_RedeemRefreshTokenOnce.given.a_refresh_token_request
{
    RejectFencedGrants _guard;
    SharedFenceStore _authority;

    async Task Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(true);
        _tokens.TryRedeemAsync(_token, Arg.Any<CancellationToken>()).Returns(true);
        _authority = new SharedFenceStore();
        await _authority.RecordFamilyStart(AuthorizationId, TokenId, FenceDoubles.Subject, _time.GetUtcNow(), _time.GetUtcNow().AddDays(30), CancellationToken.None);
        var grants = new GrantRevocation(_tokens, _authorizations, _authority, FenceDoubles.Confirmation(_authority));
        var read = new ReadRefreshGrant(_authority, _tokens, _authorizations, _time);
        _handler = new(_tokens, grants, _time, _authority, read, _issuance);
        _guard = new(_authority, _tokens, read, grants, _issuance);
    }

    async Task Because()
    {
        await _handler.HandleAsync(_context);
        await _authority.Commit(AuthorizationId, FenceDoubles.Subject, RevocationReason.Revoked, CancellationToken.None);
        await _guard.HandleAsync(_context);
    }
    [Fact] void should_have_consumed_exactly_once_before_restriction() => _tokens.Received(1).TryRedeemAsync(_token, Arg.Any<CancellationToken>());
    [Fact] void should_explicitly_reject_before_generating_the_response() => _context.Error.ShouldEqual(Errors.InvalidGrant);
}
