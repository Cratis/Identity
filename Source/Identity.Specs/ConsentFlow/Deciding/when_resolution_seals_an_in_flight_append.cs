// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_resolution_seals_an_in_flight_append : given.an_activation
{
    ConsentRequest _after;
    string? _code;

    async Task Because()
    {
        await Approve();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        evidence.BeforeRecord = async () =>
        {
            entered.SetResult();
            await release.Task;
        };
        var delayedWriter = Resume();
        await entered.Task;
        Expire();
        _after = await Restart().Resume(request.Id, CancellationToken.None);
        release.SetResult();
        await delayedWriter;
        _code = await Claim();
    }

    [Fact] void should_abandon_only_after_sealing_the_append() => _after.State.ShouldEqual(ConsentRequestState.Abandoned);
    [Fact] void should_prevent_the_late_writer_from_appending() => evidence.Appends.ShouldEqual(0);
    [Fact] void should_never_issue_a_code() => _code.ShouldBeNull();
}
