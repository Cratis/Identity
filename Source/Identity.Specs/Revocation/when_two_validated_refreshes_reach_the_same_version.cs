// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_two_validated_refreshes_reach_the_same_version : given.a_contended_refresh
{
    async Task Because()
    {
        var both = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var winnerRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loserRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        _authority.BeforeRedeem = () =>
        {
            if (Interlocked.Increment(ref arrivals) == 1)
            {
                return winnerRelease.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            }

            both.SetResult();
            return loserRelease.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        };
        await LosingGuard().HandleAsync(_validation);
        var winner = _handler.HandleAsync(_context).AsTask();
        var loser = LosingHandler().HandleAsync(_loser).AsTask();
        await both.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        winnerRelease.SetResult();
        await _consumed.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        try
        {
            loserRelease.SetResult();
            await loser;
        }
        finally
        {
            _release.SetResult();
            await winner;
        }

        _final = await _authority.Read(AuthorizationId, CancellationToken.None);
    }

    [Fact] void should_reject_the_losing_refresh() => _loser.IsRejected.ShouldBeTrue();
    [Fact] void should_leave_the_winner_admitted() => _context.IsRejected.ShouldBeFalse();
    [Fact] void should_fence_the_family() => _final.Fence.Reason.ShouldEqual(RevocationReason.RefreshTokenReused);
    [Fact] void should_leave_no_current_successor() => _final.CurrentRefreshTokenId.ShouldBeNull();
    [Fact] void should_record_reuse_only_once() => _final.Outbox.Count(entry => entry.Kind == FactKind.Reuse).ShouldEqual(1);
}
