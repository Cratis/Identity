// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.for_GitHubUserResponse;

public class when_reading_a_user_without_a_numeric_id : Specification
{
    GitHubUser? _user;

    void Because()
    {
        using var document = JsonDocument.Parse("""{ "id": "134365", "login": "einari" }""");
        _user = GitHubUserResponse.Read(document.RootElement);
    }

    [Fact] void should_not_read_a_user() => _user.ShouldBeNull();
}
