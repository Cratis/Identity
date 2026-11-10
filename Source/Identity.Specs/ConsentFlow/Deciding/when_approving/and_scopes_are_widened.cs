// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_scopes_are_widened : ConsentFlow.given.ConsentPathDriver
{
    bool _refused;
    ConsentRequestState _state;

    async Task Because()
    {
        _refused = await Refused(() => Approve(scopes: ["openid", "read", "admin"]));
        _state = await State();
    }

    [Fact] void should_refuse_the_approval() => _refused.ShouldBeTrue();
    [Fact] void should_leave_the_request_pending() => _state.ShouldEqual(ConsentRequestState.Pending);
}
