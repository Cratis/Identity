// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.RevocationSpecs;

public class when_family_adoption_loses_to_revocation : for_RedeemRefreshTokenOnce.given.a_refresh_token_request
{
    readonly TaskCompletionSource _reading = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    SharedFenceStore _authority;
    GrantState _state;

    void Establish()
    {
        _authority = new();
        var grants = new GrantRevocation(_tokens, _authorizations, _authority, FenceDoubles.Confirmation(_authority));
        var gate = new RecoveryGate();
        gate.Open(true);
        _handler = new(_tokens, grants, _time, _authority, new(_authority, _tokens, _authorizations, _time, gate), _issuance);
        _context.Transaction.GetProperty<ProcessAuthenticationContext>(typeof(ProcessAuthenticationContext).FullName).RefreshTokenPrincipal
            .SetClaim(Claims.Subject, FenceDoubles.Subject);
        _authorizations.HasStatusAsync(_authorization, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(true);
        _authorizations.GetSubjectAsync(_authorization, Arg.Any<CancellationToken>()).Returns(FenceDoubles.Subject);
        _tokens.GetCreationDateAsync(_token, Arg.Any<CancellationToken>()).Returns(_time.GetUtcNow());
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<bool>(ReadLiveEntry()));
    }

    async Task Because()
    {
        var adoption = _handler.HandleAsync(_context).AsTask();
        await _reading.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await _authority.Commit(AuthorizationId, FenceDoubles.Subject, RevocationReason.Revoked, CancellationToken.None);
        _release.SetResult();
        await adoption;
        _state = await _authority.Read(AuthorizationId, CancellationToken.None);
    }

    async Task<bool> ReadLiveEntry()
    {
        _reading.SetResult();
        await _release.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        return true;
    }

    [Fact] void should_reject_the_adopting_refresh() => _context.IsRejected.ShouldBeTrue();
    [Fact] void should_not_admit_a_successor() => _issuance.Admission.ShouldBeNull();
    [Fact] void should_not_reopen_the_family() => _state.Fence.Reason.ShouldEqual(RevocationReason.Revoked);
    [Fact] void should_not_record_a_new_family_start() => _state.Outbox.Any(entry => entry.Kind == FactKind.FamilyStarted).ShouldBeFalse();
}
