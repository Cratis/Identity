// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_fenced_grant_is_validated : for_RevokeGrantOnRefreshTokenReuse.given.a_refresh_token_being_validated
{
    RejectFencedGrants _validation;

    async Task Establish()
    {
        var fences = new SharedFenceStore();
        await fences.Commit(AuthorizationId, FenceDoubles.Subject, RevocationReason.Revoked, CancellationToken.None);
        var grants = new GrantRevocation(_tokens, _authorizations, fences, FenceDoubles.Confirmation(fences));
        _validation = new(fences, _tokens, new ReadRefreshGrant(fences, _tokens, _authorizations, TimeProvider.System), grants, new RefreshIssuance());
    }

    async Task Because() => await _validation.HandleAsync(_context);
    [Fact] void should_deny_before_built_in_validation() => _context.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_run_before_reuse_detection() => (RejectFencedGrants.Descriptor.Order < RevokeGrantOnRefreshTokenReuse.Descriptor.Order).ShouldBeTrue();
    [Fact] void should_not_wait_for_token_cleanup() => _tokens.DidNotReceive().RevokeByAuthorizationIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
