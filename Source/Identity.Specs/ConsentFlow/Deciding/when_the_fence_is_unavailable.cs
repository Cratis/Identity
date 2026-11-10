// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_the_fence_is_unavailable : given.an_activation
{
    ConsentRequest _after;
    string? _code;

    async Task Because()
    {
        await Approve();
        fence.Fail = true;
        _after = await Resume();
        _code = await Claim();
    }

    [Fact] void should_fail_closed() => _after.State.ShouldEqual(ConsentRequestState.Abandoned);
    [Fact] void should_not_append_evidence() => evidence.Appends.ShouldEqual(0);
    [Fact] void should_not_issue_a_code() => _code.ShouldBeNull();
}
