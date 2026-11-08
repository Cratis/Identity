// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_linked_key_has_another_link_id : given.a_sign_in_decision
{
    void Establish() => _account = _account with
    {
        LinkedUpstreamKeys = [_account.LinkedUpstreamKeys.Single() with { LinkId = new LinkId(Guid.Parse("11111111-1111-1111-1111-111111111111")) }]
    };

    Task Because() => Decide();

    [Fact] void should_not_accept_the_same_upstream_pair_under_another_link() => _refusal.ShouldEqual("upstream_key_reserved_but_inactive");
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
}
