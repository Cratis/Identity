// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_an_issuer_has_no_audience : given.a_token_exchange_configuration
{
    void Establish() => Issuer.Audiences = [];
    void Because() => Validate();

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
}
