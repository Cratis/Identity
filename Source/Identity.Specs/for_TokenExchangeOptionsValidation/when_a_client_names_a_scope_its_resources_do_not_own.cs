// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_a_client_names_a_scope_its_resources_do_not_own : given.a_token_exchange_configuration
{
    void Establish() => Client.Scopes = ["studio:read"];
    void Because() => Validate();

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
}
