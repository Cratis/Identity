// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_a_jwks_uri_is_http_outside_development : given.a_token_exchange_configuration
{
    void Establish() => Issuer.JwksUri = "http://login.partner.example/jwks.json";
    void Because() => Validate();

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
}
