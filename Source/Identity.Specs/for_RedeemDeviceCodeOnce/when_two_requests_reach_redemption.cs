// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_RedeemDeviceCodeOnce;

public class when_two_requests_reach_redemption : given.a_device_redemption
{
    ProcessSignInContext _second;
    int _arrived;
    readonly TaskCompletionSource _both = new(TaskCreationOptions.RunContinuationsAsynchronously);
    GrantState? _final;

    void Establish()
    {
        _second = Request();
        _tokens.TryRedeemAsync(_token, Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<bool>(Redeem()));
    }

    async Task Because()
    {
        await Task.WhenAll(_handler.HandleAsync(_context).AsTask(), _handler.HandleAsync(_second).AsTask());
        _final = await _authority.Read("authorization", CancellationToken.None);
    }

    async Task<bool> Redeem()
    {
        var arrival = Interlocked.Increment(ref _arrived);
        if (arrival == 2) _both.TrySetResult();
        await _both.Task.WaitAsync(TimeSpan.FromSeconds(5), _clock);
        return arrival == 1;
    }

    [Fact] void should_have_admitted_a_live_grant_before_the_race() => _baseline.ShouldBeTrue();
    [Fact] void should_reach_the_cas_boundary_twice() => _arrived.ShouldEqual(2);
    [Fact] void should_admit_exactly_one_request() => new[] { _context, _second }.Count(_ => !_.IsRejected).ShouldEqual(1);
    [Fact] void should_reject_the_other_as_invalid_grant() => new[] { _context, _second }.Single(_ => _.IsRejected).Error.ShouldEqual("invalid_grant");
    [Fact] void should_permanently_fence_the_reused_grant() => _final.Fence.Reason.ShouldEqual(RevocationReason.Revoked);
}
