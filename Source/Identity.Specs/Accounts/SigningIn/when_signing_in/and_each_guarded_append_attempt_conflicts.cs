// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Identity.Accounts.Lookup;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_each_guarded_append_attempt_conflicts : given.conflicting_guarded_decisions
{
    async Task Because()
    {
        var signIn = SignIn();
        for (var fence = 0; fence < 3; fence++)
        {
            await _accountReached[fence].Task.WaitAsync(TimeSpan.FromSeconds(5), _clock);
            ObservePendingReads();
            _accountPositions[fence].SetResult(new(ObserverRunningState.Active, false, false, Target(fence, typeof(Account))));
            await _lookupReached[fence].Task.WaitAsync(TimeSpan.FromSeconds(5), _clock);
            ObservePendingReads();
            _lookupPositions[fence].SetResult(new(ObserverRunningState.Active, false, false, Target(fence, typeof(UpstreamKeyLookup))));
        }

        await signIn.WaitAsync(TimeSpan.FromSeconds(5), _clock);
    }

    [Fact] void should_capture_both_projection_targets_for_each_decision_and_the_retry_fence() => _targets.ShouldEqual([(0, typeof(Account)), (0, typeof(UpstreamKeyLookup)), (1, typeof(Account)), (1, typeof(UpstreamKeyLookup)), (2, typeof(Account)), (2, typeof(UpstreamKeyLookup))]);
    [Fact] void should_not_recheck_read_accounts_or_produce_events_until_both_barriers_complete() => _pendingReads.ShouldEqual([(1, 0, 0), (1, 0, 0), (2, 1, 1), (2, 1, 1), (3, 1, 1), (3, 1, 1)]);
    [Fact] void should_run_exactly_two_whole_decisions() => _attempts.Count.ShouldEqual(2);
    [Fact] void should_use_a_new_command_for_the_retry() => ReferenceEquals(_attempts[0], _attempts[1]).ShouldBeFalse();
    [Fact] void should_produce_a_fresh_event_batch_on_each_attempt() => _decisionEvents.Select(events => events.Length).ShouldEqual([1, 1]);
    [Fact] void should_wait_once_between_attempts() => _commands.Received(1).BeforeRetry(Arg.Any<CancellationToken>());
    [Fact] void should_stop_after_retry_exhaustion() => _refusal.ShouldEqual("account_concurrency_conflict");
    [Fact] void should_not_return_a_session_account() => _signedIn.ShouldBeNull();
}
