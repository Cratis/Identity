// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_the_approval_races_a_revocation : given.an_activation
{
    ConsentRequest _after;
    string? _code;
    bool _usable;

    async Task Because()
    {
        await Approve();
        authorizations.BeforePrepare = () => fence.Clear = false;
        _after = await Resume();
        _code = await Claim();
        _usable = await authorizations.Usable(request.Id, store, fence);
    }

    [Fact] void should_withhold_the_evidenced_request() => _after.State.ShouldEqual(ConsentRequestState.EvidenceWithheld);
    [Fact] void should_never_issue_a_code_after_the_fence() => _code.ShouldBeNull();
    [Fact] void should_never_expose_a_usable_authorization() => _usable.ShouldBeFalse();
}
