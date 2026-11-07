// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs;

public class when_revocation_arrives_before_consumption : for_RedeemRefreshTokenOnce.given.a_refresh_token_request
{
    RejectFencedGrants _guard;
    SharedFenceStore _authority;
    GrantState _state;

    async Task Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(true);
        _authority = new SharedFenceStore();
        await _authority.RecordFamilyStart(AuthorizationId, TokenId, FenceDoubles.Subject, _time.GetUtcNow(), _time.GetUtcNow().AddDays(30), CancellationToken.None);
        var grants = new GrantRevocation(_tokens, _authorizations, _authority, FenceDoubles.Confirmation(_authority));
        var read = new ReadRefreshGrant(_authority, _tokens, _authorizations, _time);
        _handler = new(_tokens, grants, _time, _authority, read, _issuance);
        _guard = new(_authority, _tokens, read, grants, _issuance);
    }

    async Task Because()
    {
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _authority.BeforeRedeem = () =>
        {
            held.SetResult();
            return release.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        };
        var refresh = _handler.HandleAsync(_context);
        try
        {
            await held.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            await _authority.Commit(AuthorizationId, FenceDoubles.Subject, RevocationReason.Revoked, CancellationToken.None);
        }
        finally
        {
            release.SetResult();
        }

        await refresh;
        await _guard.HandleAsync(_context);
        _state = await _authority.Read(AuthorizationId, CancellationToken.None);
    }

    [Fact] void should_reject_the_refresh() => _context.IsRejected.ShouldBeTrue();
    [Fact] void should_return_invalid_grant() => _context.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_not_admit_a_response() => _issuance.Admission.ShouldBeNull();
    [Fact] void should_not_install_a_successor() => _state.CurrentRefreshTokenId.ShouldBeNull();
    [Fact] void should_not_redeem_the_protocol_entry() => _tokens.DidNotReceive().TryRedeemAsync(_token, Arg.Any<CancellationToken>());
}
