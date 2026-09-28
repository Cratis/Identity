// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.for_GitHubUserResponse;

public class when_reading_a_user_without_a_name : Specification
{
    GitHubUser? _user;

    void Because()
    {
        using var document = JsonDocument.Parse("""{ "id": 134365, "login": "einari", "name": null }""");
        _user = GitHubUserResponse.Read(document.RootElement);
    }

    [Fact] void should_read_the_numeric_id() => _user!.Id.ShouldEqual(134365L);
    [Fact] void should_read_the_login() => _user!.Login.ShouldEqual("einari");
    [Fact] void should_use_the_login_as_name() => _user!.Name.ShouldEqual("einari");
}
