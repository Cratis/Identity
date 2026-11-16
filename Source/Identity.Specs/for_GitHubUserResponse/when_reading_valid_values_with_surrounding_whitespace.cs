// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.for_GitHubUserResponse;

public class when_reading_valid_values_with_surrounding_whitespace : Specification
{
    GitHubUser? _user;

    void Because()
    {
        using var document = JsonDocument.Parse("""{ "id": 9223372036854775807, "login": " synthetic-login ", "name": " Synthetic Name ", "email": " profile@example.invalid " }""");
        _user = GitHubUserResponse.Read(document.RootElement);
    }

    [Fact] void should_read_the_maximum_integer_id() => _user.Id.ShouldEqual(long.MaxValue);
    [Fact] void should_keep_the_login_unchanged() => _user.Login.ShouldEqual(" synthetic-login ");
    [Fact] void should_keep_the_name_unchanged() => _user.Name.ShouldEqual(" Synthetic Name ");
    [Fact] void should_keep_the_public_email_unchanged() => _user.Email.Value.ShouldEqual(" profile@example.invalid ");
}
