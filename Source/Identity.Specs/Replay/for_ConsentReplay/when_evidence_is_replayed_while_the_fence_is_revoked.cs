// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Replay;

public class when_evidence_is_replayed_while_the_fence_is_revoked : given.a_replayed_activation
{
    int _prepares;

    async Task Because()
    {
        await Approve();
        await Resume();
        _prepares = counting.Prepares;
        fence.Clear = false;
        await ReplayTwice();
    }

    [Fact] void should_keep_the_active_record_without_bypassing_revocation() => after.State.ShouldEqual(ConsentRequestState.Active);
    [Fact] void should_not_prepare_again() => counting.Prepares.ShouldEqual(_prepares);
    [Fact] void should_not_restore_a_revoked_grant() => usable.ShouldBeFalse();
    [Fact] void should_not_issue_a_code_after_revocation() => code.ShouldBeNull();
}
