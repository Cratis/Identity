// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_denying : given.an_activation
{
    ConsentRequest _after;
    string? _code;

    async Task Because()
    {
        await activation.Decide(request.Id, binding, false, CancellationToken.None);
        await Approve();
        _after = await Resume();
        _code = await Claim();
    }

    [Fact] void should_keep_the_terminal_denial() => _after.State.ShouldEqual(ConsentRequestState.Denied);
    [Fact] void should_append_nothing() => evidence.Appends.ShouldEqual(0);
    [Fact] void should_prepare_nothing() => authorizations.Count.ShouldEqual(0);
    [Fact] void should_issue_nothing() => _code.ShouldBeNull();
}
