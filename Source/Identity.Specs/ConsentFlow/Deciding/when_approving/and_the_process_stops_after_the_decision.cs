// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_the_process_stops_after_the_decision : given.an_activation
{
    ConsentRequest _before;
    ConsentRequest _after;
    string? _code;
    int _appendsBefore;

    async Task Because()
    {
        _before = await Approve();
        _code = await Claim();
        _appendsBefore = evidence.Appends;
        activation = Restart();
        _after = await Resume();
    }

    [Fact] void should_leave_a_durable_decision() => _before.State.ShouldEqual(ConsentRequestState.Decided);
    [Fact] void should_not_issue_a_code_before_activation() => _code.ShouldBeNull();
    [Fact] void should_not_append_during_the_decision() => _appendsBefore.ShouldEqual(0);
    [Fact] void should_resume_to_active() => _after.State.ShouldEqual(ConsentRequestState.Active);
    [Fact] void should_append_once() => evidence.Appends.ShouldEqual(1);
    [Fact] void should_prepare_one_grant() => authorizations.Count.ShouldEqual(1);
}
