// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_a_client_references_an_unknown_resource : given.a_token_exchange_configuration
{
    void Establish() => Client.Resources = ["unknown-api"];
    void Because() => Validate();

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
    [Fact] void should_name_the_client_and_the_resource() => result.FailureMessage.ShouldContain($"\"{Client.ClientId}\" references a resource \"unknown-api\" that is not configured or was disabled");
}
