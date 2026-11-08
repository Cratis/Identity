// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using OpenIddict.Server;

namespace Cratis.Identity.RevocationSpecs;

public class when_an_rfc7009_retry_cannot_confirm_the_log : for_RevokeGrantOnRefreshTokenReuse.given.a_refresh_token_being_validated
{
    RejectFencedGrants _validation;
    Exception? _error;

    async Task Establish()
    {
        _context.Transaction.EndpointType = OpenIddictServerEndpointType.Revocation;
        var fences = new SharedFenceStore();
        await fences.Commit(AuthorizationId, FenceDoubles.Subject, RevocationReason.RefreshTokenRevoked, CancellationToken.None);
        var log = Substitute.For<IFenceLog>();
        log.Append(Arg.Any<GrantFence>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<long>(new RevocationPending()));
        var grants = new GrantRevocation(_tokens, _authorizations, fences, new ConfirmFenceLog(fences, log));
        _validation = new(fences, _tokens, new ReadRefreshGrant(fences, _tokens, _authorizations, TimeProvider.System), grants, new RefreshIssuance());
    }

    async Task Because() => _error = await Catch.Exception(async () => await _validation.HandleAsync(_context));
    [Fact] void should_fail_the_request_instead_of_acknowledging() => _error.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_return_an_invalid_token_that_rfc7009_converts_to_success() => _context.IsRejected.ShouldBeFalse();
}
