// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_a_client_id_is_the_cratis_cli : given.a_token_exchange_configuration
{
    void Establish() => Client.ClientId = ClientDescriptors.CratisCli;
    void Because() => Validate();

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
}
