// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_AuthorizationParameters;

public class when_returning_after_login_with_only_a_login_prompt : Specification
{
    IEnumerable<KeyValuePair<string, string>> _result;

    void Because() => _result = AuthorizationParameters.AfterLogin([new("client_id", "cratis-cli"), new("prompt", "login")]);

    [Fact] void should_remove_the_prompt_parameter() => _result.Any(_ => _.Key == "prompt").ShouldBeFalse();
}
