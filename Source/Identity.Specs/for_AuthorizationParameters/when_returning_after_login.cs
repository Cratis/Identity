// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_AuthorizationParameters;

public class when_returning_after_login : Specification
{
    IEnumerable<KeyValuePair<string, string>> _result;

    void Because() => _result = AuthorizationParameters.AfterLogin(
    [
        new("client_id", "cratis-cli"),
        new("prompt", "login consent"),
        new("max_age", "300"),
        new("resource", "https://cratis.direct/mcp"),
        new("resource", "https://cratis.direct/v1")
    ]);

    [Fact] void should_remove_the_login_prompt_but_keep_other_prompts() => _result.Single(_ => _.Key == "prompt").Value.ShouldEqual("consent");
    [Fact] void should_keep_repeated_parameters() => _result.Count(_ => _.Key == "resource").ShouldEqual(2);
    [Fact] void should_remove_max_age_because_the_user_just_authenticated() => _result.Any(_ => _.Key == "max_age").ShouldBeFalse();
    [Fact] void should_keep_the_client() => _result.ShouldContain(new KeyValuePair<string, string>("client_id", "cratis-cli"));
}
