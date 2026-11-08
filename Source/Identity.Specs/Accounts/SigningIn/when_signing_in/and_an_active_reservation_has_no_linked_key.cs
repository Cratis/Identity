// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_an_active_reservation_has_no_linked_key : given.a_sign_in_decision
{
    void Establish() => _account = _account with { LinkedUpstreamKeys = [] };

    Task Because() => Decide();

    [Fact] void should_fail_closed() => _refusal.ShouldEqual("upstream_key_reserved_but_inactive");
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
}
