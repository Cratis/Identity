// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.for_GitHubUserResponse;

public class when_the_optional_public_email_is_unavailable : Specification
{
    readonly List<GitHubUser?> _users = [];

    void Because()
    {
        string[] responses =
        [
            /*lang=json*/ """{ "id": 42, "login": "synthetic-login" }""",
            /*lang=json*/ """{ "id": 42, "login": "synthetic-login", "email": null }""",
            /*lang=json*/ """{ "id": 42, "login": "synthetic-login", "email": "" }""",
            /*lang=json*/ """{ "id": 42, "login": "synthetic-login", "email": "   " }""",
            /*lang=json*/ """{ "id": 42, "login": "synthetic-login", "email": 42 }""",
            /*lang=json*/ """{ "id": 42, "login": "synthetic-login", "email": {} }"""
        ];
        foreach (var response in responses)
        {
            using var document = JsonDocument.Parse(response);
            _users.Add(GitHubUserResponse.Read(document.RootElement));
        }
    }

    [Fact] void should_still_read_the_immutable_user_id() => _users.TrueForAll(user => user?.Id == 42).ShouldBeTrue();
    [Fact] void should_not_invent_an_email() => _users.TrueForAll(user => user.Email is null).ShouldBeTrue();
}
