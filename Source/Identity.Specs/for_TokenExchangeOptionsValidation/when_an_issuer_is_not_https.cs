// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_an_issuer_is_not_https : given.a_token_exchange_configuration
{
    void Establish()
    {
        environmentName = "Development";
        Issuer.Issuer = "http://login.partner.example";
        Client.Issuers = [Issuer.Issuer];
    }

    void Because() => Validate();

    [Fact] void should_fail_even_in_development() => result.Failed.ShouldBeTrue();
}
