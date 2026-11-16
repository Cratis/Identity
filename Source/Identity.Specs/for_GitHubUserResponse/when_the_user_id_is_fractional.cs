// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.for_GitHubUserResponse;

public class when_the_user_id_is_fractional : Specification
{
    GitHubUser? _user;

    void Because()
    {
        using var document = JsonDocument.Parse("""{ "id": 1.5, "login": "synthetic-login" }""");
        _user = GitHubUserResponse.Read(document.RootElement);
    }

    [Fact] void should_not_read_a_user() => _user.ShouldBeNull();
}
