// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.RevocationSpecs;

public class when_reuse_is_detected_after_the_winner_responds : given.a_contended_refresh
{
    ProcessSignInContext _response;

    async Task Because()
    {
        await LosingGuard().HandleAsync(_validation);
        var winner = _handler.HandleAsync(_context).AsTask();
        await _consumed.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        _release.SetResult();
        await winner;
        _response = new ProcessSignInContext(new OpenIddictServerTransaction { Request = _context.Request, EndpointType = OpenIddictServerEndpointType.Token });
        await new RejectFencedGrants(_authority, _tokens, _read, _grants, _issuance).HandleAsync(_response);
        await LosingHandler().HandleAsync(_loser);
        _final = await _authority.Read(AuthorizationId, CancellationToken.None);
    }

    [Fact] void should_allow_the_winning_response() => _response.IsRejected.ShouldBeFalse();
    [Fact] void should_reject_the_losing_request() => _loser.IsRejected.ShouldBeTrue();
    [Fact] void should_fence_the_already_issued_family() => _final.Fence.Reason.ShouldEqual(RevocationReason.RefreshTokenReused);
    [Fact] void should_clear_the_issued_successor() => _final.CurrentRefreshTokenId.ShouldBeNull();
}
