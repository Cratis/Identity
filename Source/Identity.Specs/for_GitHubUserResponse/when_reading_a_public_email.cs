// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.for_GitHubUserResponse;

public class when_reading_a_public_email : Specification
{
    GitHubUser _user;

    void Because()
    {
        using var document = JsonDocument.Parse("""{ "id": 42, "login": "synthetic-login", "email": "profile@example.invalid" }""");
        _user = GitHubUserResponse.Read(document.RootElement);
    }

    [Fact] void should_read_the_public_email() => _user.Email.Value.ShouldEqual("profile@example.invalid");
    [Fact] void should_keep_the_email_in_a_pii_marked_concept() => Attribute.IsDefined(typeof(Email), typeof(PIIAttribute)).ShouldBeTrue();
}
