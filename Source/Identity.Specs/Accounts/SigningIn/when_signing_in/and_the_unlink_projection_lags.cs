// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_unlink_projection_lags : given.a_sign_in_decision
{
    void Establish() => _releaseProjection = () =>
    {
        _reservation = _reservation with { Active = false };
        _account = _account with { LinkedUpstreamKeys = [] };
    };

    Task Because() => Decide();

    [Fact] void should_refuse_on_the_first_attempt() => _refusal.ShouldEqual("upstream_key_reserved_but_inactive");
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
    [Fact] void should_enroll_before_waiting_and_recheck_after_waiting() => _order.ShouldEqual(["reservation", "guard", "target", "target", "reservation", "guard", "account"]);
}
