// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_a_client_references_an_unknown_issuer : given.a_token_exchange_configuration
{
    void Establish() => Client.Issuers = ["https://other.example"];
    void Because() => Validate();

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
}
