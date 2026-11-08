// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Replay;

public class when_evidence_is_replayed_for_an_abandoned_request : given.a_replayed_activation
{
    async Task Because()
    {
        await Approve();
        Expire();
        await Resume(); // Absence is sealed before a late append can arrive.
        await ReplayTwice();
    }

    [Fact] void should_stay_abandoned() => after.State.ShouldEqual(ConsentRequestState.Abandoned);
    [Fact] void should_not_prepare_an_authorization() => counting.Prepares.ShouldEqual(0);
    [Fact] void should_not_make_the_grant_usable() => usable.ShouldBeFalse();
    [Fact] void should_not_issue_a_code() => code.ShouldBeNull();
}
