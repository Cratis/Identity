// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_verified_sign_in_is_missing : given.a_sign_in_decision
{
    void Establish() => _verified = new();
    Task Because() => Decide();
    [Fact] void should_refuse() => _refusal.ShouldEqual("verified_sign_in_required");
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
    [Fact] void should_not_enroll_a_guard() => _order.ShouldBeEmpty();
}
