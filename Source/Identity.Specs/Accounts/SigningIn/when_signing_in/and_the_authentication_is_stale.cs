// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_authentication_is_stale : given.a_sign_in_decision
{
    void Establish() => _clock.Now += TimeSpan.FromMinutes(6);
    Task Because() => Decide();
    [Fact] void should_refuse() => _refusal.ShouldEqual("fresh_upstream_authentication_required");
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
}
