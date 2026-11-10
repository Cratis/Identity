// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow.Pending;

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_the_session_changes : ConsentFlow.given.ConsentPathDriver
{
    bool _refused;
    PendingConsent? _shown;
    ConsentRequestState _state;

    async Task Because()
    {
        As("session-b");
        _refused = await Refused(() => Approve());
        _shown = await Pending();
        _state = await State();
    }

    [Fact] void should_refuse_the_approval() => _refused.ShouldBeTrue();
    [Fact] void should_not_show_the_request_to_the_new_session() => _shown.ShouldBeNull();
    [Fact] void should_leave_the_request_pending() => _state.ShouldEqual(ConsentRequestState.Pending);
    [Fact] void should_append_nothing() => AppendedFacts.ShouldEqual(0);
}
