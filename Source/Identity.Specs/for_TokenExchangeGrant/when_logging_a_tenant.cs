// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_logging_a_tenant : Specification
{
    string _short;
    string _long;

    void Because()
    {
        _short = TokenExchangeGrant.SafeForLog("cratis");
        _long = TokenExchangeGrant.SafeForLog(new string('x', 5000));
    }

    [Fact] void should_keep_a_short_tenant_as_it_is() => _short.ShouldEqual("cratis");
    [Fact] void should_truncate_a_long_tenant() => (_long.Length <= TokenExchangeGrant.MaximumLoggedLength + 1).ShouldBeTrue();
}
