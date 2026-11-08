// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_validating_a_valid_configuration : given.a_token_exchange_configuration
{
    void Because() => Validate();

    [Fact] void should_succeed() => result.Succeeded.ShouldBeTrue();
}
