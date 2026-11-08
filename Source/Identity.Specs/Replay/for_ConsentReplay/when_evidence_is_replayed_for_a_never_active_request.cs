// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Replay;

public class when_evidence_is_replayed_for_a_never_active_request : given.a_replayed_activation
{
    bool _pendingUsable;
    string? _pendingCode;

    async Task Because()
    {
        await ReplayTwice();
        _pendingUsable = usable;
        _pendingCode = code;
        await Approve();
        counting.Withhold = true;
        await ReplayTwice();
    }

    [Fact] void should_not_activate_pending_evidence() => _pendingUsable.ShouldBeFalse();
    [Fact] void should_not_issue_a_code_for_pending_evidence() => _pendingCode.ShouldBeNull();
    [Fact] void should_remain_never_active_without_preparation() => after.State.ShouldEqual(ConsentRequestState.EvidenceRecorded);
    [Fact] void should_not_make_decided_evidence_usable() => usable.ShouldBeFalse();
    [Fact] void should_not_issue_a_code_without_activation() => code.ShouldBeNull();
}
