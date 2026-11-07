// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_linked_key_has_another_issuer : given.a_sign_in_decision
{
    void Establish() => _account = _account with
    {
        LinkedUpstreamKeys = [_account.LinkedUpstreamKeys.Single() with { IdpIssuer = "https://another-issuer.example.test" }]
    };

    Task Because() => Decide();

    [Fact] void should_require_the_exact_issuer_even_for_the_same_subject_and_link() => _refusal.ShouldEqual("upstream_key_reserved_but_inactive");
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
}
