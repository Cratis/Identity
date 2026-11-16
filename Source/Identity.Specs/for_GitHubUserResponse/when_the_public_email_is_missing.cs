// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.for_GitHubUserResponse;

public class when_the_public_email_is_missing : Specification
{
    GitHubUser? _user;

    void Because()
    {
        using var document = JsonDocument.Parse("""{ "id": 42, "login": "synthetic-login" }""");
        _user = GitHubUserResponse.Read(document.RootElement);
    }

    [Fact] void should_still_read_the_user() => _user.Id.ShouldEqual(42L);
    [Fact] void should_not_invent_an_email() => _user.Email.ShouldBeNull();
}
