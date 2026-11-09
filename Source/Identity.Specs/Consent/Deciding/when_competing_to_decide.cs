// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_competing_to_decide : Pending.given.a_consent_interaction
{
    Exception?[] _results;
    ConsentRequest? _after;

    async Task Because()
    {
        var arrivals = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store.BeforeCompareExchange = async (_, _) =>
        {
            if (Interlocked.Increment(ref arrivals) == 3) release.SetResult();
            await release.Task;
        };
        _results = await Task.WhenAll(Record.ExceptionAsync(() => Approve()).AsTask(), Record.ExceptionAsync(() => Deny()).AsTask(), Record.ExceptionAsync(() => Approve()).AsTask());
        _after = await store.Read(request.Id, CancellationToken.None);
    }

    [Fact] void should_allow_exactly_one_decision() => _results.Count(_ => _ is null).ShouldEqual(1);
    [Fact] void should_uniformly_refuse_the_loser() => _results.Count(_ => _ is ConsentDecisionRefused).ShouldEqual(2);
    [Fact] void should_only_increment_once() => _after.Revision.ShouldEqual(1);
    [Fact] void should_only_write_one_pending_transition() => store.Attempts.Count(_ => _.Won).ShouldEqual(1);
    [Fact] void should_never_write_evidence() => evidence.Appends.ShouldEqual(0);
    [Fact] void should_never_prepare_a_grant() => authorizations.Count.ShouldEqual(0);
}
