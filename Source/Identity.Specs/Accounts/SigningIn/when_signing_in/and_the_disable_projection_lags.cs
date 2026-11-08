// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_disable_projection_lags : given.a_sign_in_decision
{
    void Establish() => _releaseProjection = () => _account = _account with { Status = AccountStatus.Disabled };

    Task Because() => Decide();

    [Fact] void should_refuse_on_the_first_attempt() => _refusal.ShouldEqual("account_not_active");
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
}
