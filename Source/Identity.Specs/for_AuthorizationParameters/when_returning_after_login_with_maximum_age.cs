// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_AuthorizationParameters;

public class when_returning_after_login_with_maximum_age : Specification
{
    IEnumerable<KeyValuePair<string, string>> _forced;
    IEnumerable<KeyValuePair<string, string>> _stale;

    void Because()
    {
        _forced = AuthorizationParameters.AfterLogin([new("max_age", "0"), new("prompt", "login consent"), new("nonce", "request-nonce")]);
        _stale = AuthorizationParameters.AfterLogin([new("max_age", "60"), new("client_id", "oidc-client")]);
    }

    [Fact] void should_consume_the_forced_reauthentication_requirement() => _forced.Any(_ => _.Key == "max_age").ShouldBeFalse();
    [Fact] void should_consume_the_satisfied_positive_maximum_age() => _stale.Any(_ => _.Key == "max_age").ShouldBeFalse();
    [Fact] void should_also_consume_the_login_prompt() => _forced.Single(_ => _.Key == "prompt").Value.ShouldEqual("consent");
    [Fact] void should_preserve_the_nonce() => _forced.Single(_ => _.Key == "nonce").Value.ShouldEqual("request-nonce");
    [Fact] void should_preserve_the_client() => _stale.Single(_ => _.Key == "client_id").Value.ShouldEqual("oidc-client");
}
