// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_revocation_follows_activation : given.an_activation
{
    bool _usable;
    string? _code;

    async Task Because()
    {
        await Approve();
        await Resume();
        fence.Clear = false;
        activation = Restart();
        _usable = await authorizations.Usable(request.Id, store, fence);
        _code = await Claim();
    }

    [Fact] void should_not_treat_active_as_bypassing_a_fence() => _usable.ShouldBeFalse();
    [Fact] void should_not_issue_a_code_after_revocation() => _code.ShouldBeNull();
}
