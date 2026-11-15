// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_missing_grant_cannot_be_proven_live : for_RedeemRefreshTokenOnce.given.a_refresh_token_request
{
    void Establish() => _fences.Read(AuthorizationId, Arg.Any<CancellationToken>()).Returns((GrantState?)null);
    async Task Because() => await _handler.HandleAsync(_context);
    [Fact] void should_deny_unknown_family_state() => _context.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_not_install_a_successor() => _issuance.SuccessorId.ShouldBeNull();
    [Fact] void should_not_create_permission_from_absence() => _fences.DidNotReceive().RecordFamilyStart(Arg.Any<FamilyStart>(), Arg.Any<CancellationToken>());
}
