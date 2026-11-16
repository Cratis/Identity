// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.for_GitHubUserResponse;

public class when_the_name_is_whitespace : Specification
{
    GitHubUser? _user;

    void Because()
    {
        using var document = JsonDocument.Parse("""{ "id": 42, "login": "synthetic-login", "name": "   " }""");
        _user = GitHubUserResponse.Read(document.RootElement);
    }

    [Fact] void should_use_the_login_as_name() => _user.Name.ShouldEqual("synthetic-login");
}
