// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_denying_through_the_command : ConsentFlow.given.ConsentPathDriver
{
    ConsentRequestState _state;
    bool _usable;
    string? _code;

    async Task Because()
    {
        await Deny();
        await Reconcile();
        _state = await State();
        _usable = await AuthorizationUsable();
        _code = await ClaimCode();
    }

    [Fact] void should_keep_the_terminal_denial() => _state.ShouldEqual(ConsentRequestState.Denied);
    [Fact] void should_append_nothing() => AppendedFacts.ShouldEqual(0);
    [Fact] void should_prepare_nothing() => PreparedAuthorizations.ShouldEqual(0);
    [Fact] void should_not_be_usable() => _usable.ShouldBeFalse();
    [Fact] void should_issue_nothing() => _code.ShouldBeNull();
}
