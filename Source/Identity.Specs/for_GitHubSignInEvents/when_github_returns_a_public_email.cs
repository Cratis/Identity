// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_github_returns_a_public_email : given.a_github_callback
{
    Task Because() => CreateTicket("""{ "id": 42, "login": "synthetic-login", "name": "Synthetic User", "email": "profile@example.invalid" }""");

    [Fact] void should_forward_the_email_as_a_profile_observation() => _profile.Email.Value.ShouldEqual("profile@example.invalid");
    [Fact] void should_use_the_canonical_account_for_the_session() => SessionPrincipal.AccountId(_context.Principal).ShouldEqual(_account.Id.Value);
    [Fact] void should_not_put_the_email_in_the_session() => _context.Principal.Claims.Any(claim => claim.Value == "profile@example.invalid").ShouldBeFalse();
}
