// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_token_exchange_is_not_configured : given.a_token_exchange_configuration
{
    IdentityOptions _defaults;

    void Because()
    {
        _defaults = new();
        options.TokenExchange = _defaults.TokenExchange;
        Validate();
    }

    [Fact] void should_have_no_issuers_by_default() => _defaults.TokenExchange.Issuers.ShouldBeEmpty();
    [Fact] void should_have_no_clients_by_default() => _defaults.TokenExchange.Clients.ShouldBeEmpty();
    [Fact] void should_succeed() => result.Succeeded.ShouldBeTrue();
}
