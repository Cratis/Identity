// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_the_process_stops_after_the_evidence_append : given.an_activation
{
    ConsentRequest _before;
    ConsentRequest _after;
    string? _code;

    async Task Because()
    {
        await Approve();
        store.StopBefore = ConsentRequestState.EvidenceRecorded;
        try { await Resume(); } catch (InvalidOperationException) { }
        _before = await store.Read(request.Id, CancellationToken.None);
        _code = await Claim();
        activation = Restart();
        _after = await Resume();
    }

    [Fact] void should_leave_the_operational_decision_unadvanced() => _before.State.ShouldEqual(ConsentRequestState.Decided);
    [Fact] void should_issue_nothing_before_the_barrier() => _code.ShouldBeNull();
    [Fact] void should_recover_without_a_duplicate_append() => evidence.Appends.ShouldEqual(1);
    [Fact] void should_resume_to_active() => _after.State.ShouldEqual(ConsentRequestState.Active);
}
