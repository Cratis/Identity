// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_an_issuer_is_configured_twice : given.a_token_exchange_configuration
{
    void Establish() => options.TokenExchange.Issuers.Add(new ExternalIssuerOptions { Issuer = Issuer.Issuer, JwksUri = Issuer.JwksUri, Audiences = ["x"] });
    void Because() => Validate();

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
}
