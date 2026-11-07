// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_github_returns_no_public_email : given.a_github_callback
{
    Task Because() => CreateTicket("""{ "id": 42, "login": "synthetic-login", "name": "Synthetic User", "email": null }""");

    [Fact] void should_not_invent_an_email_observation() => _profile.Email.ShouldBeNull();
    [Fact] void should_keep_sign_in_bound_to_the_canonical_account() => SessionPrincipal.AccountId(_context.Principal).ShouldEqual(_account.Id.Value);
    [Fact] void should_not_add_an_email_claim() => _context.Principal.FindFirst("email").ShouldBeNull();
}
