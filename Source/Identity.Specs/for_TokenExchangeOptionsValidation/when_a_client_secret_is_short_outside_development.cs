// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_a_client_secret_is_short_outside_development : given.a_token_exchange_configuration
{
    void Establish() => Client.ClientSecret = new string('s', 31);
    void Because() => Validate();

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
}
