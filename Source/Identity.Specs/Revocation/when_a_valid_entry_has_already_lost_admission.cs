// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_valid_entry_has_already_lost_admission : given.a_contended_refresh
{
    ProcessSignInContext _response;
    IResult _endpointResult;

    async Task Because()
    {
        // The fence CAS has consumed the parent, but its OpenIddict entry is still valid.
        var winner = _handler.HandleAsync(_context).AsTask();
        await _consumed.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        try
        {
            await LosingGuard().HandleAsync(_validation);
            if (!_validation.IsRejected)
            {
                _endpointResult = await ReachTokenEndpoint();
                if (_endpointResult is SignInHttpResult)
                {
                    await LosingHandler().HandleAsync(_loser);
                }
            }
        }
        finally
        {
            _release.SetResult();
            await winner;
        }

        _response = new ProcessSignInContext(new OpenIddictServerTransaction { Request = _context.Request, EndpointType = OpenIddictServerEndpointType.Token });
        await new RejectFencedGrants(_authority, _tokens, _read, _grants, _issuance).HandleAsync(_response);
        _final = await _authority.Read(AuthorizationId, CancellationToken.None);
    }

    [Fact] void should_route_the_valid_loser_to_reuse_detection() => _validation.IsRejected.ShouldBeFalse();
    [Fact] void should_reach_redemption_through_the_endpoint() => _endpointResult.ShouldBeOfExactType<SignInHttpResult>();
    [Fact] void should_reject_the_loser() => _loser.IsRejected.ShouldBeTrue();
    [Fact] void should_not_allocate_a_losing_successor() => _loserIssuance.SuccessorId.ShouldBeNull();
    [Fact] void should_fence_the_family_for_reuse() => _final.Fence.Reason.ShouldEqual(RevocationReason.RefreshTokenReused);
    [Fact] void should_emit_one_reuse_fact() => _final.Outbox.Count(entry => entry.Kind == FactKind.Reuse).ShouldEqual(1);
    [Fact] void should_allow_the_already_admitted_winner() => _response.IsRejected.ShouldBeFalse();
}
