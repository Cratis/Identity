// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_a_client_id_collides_with_an_introspection_client : given.a_token_exchange_configuration
{
    void Establish() => options.IntrospectionClients.Add(new IntrospectionClientOptions { ClientId = Client.ClientId, ClientSecret = "secret" });
    void Because() => Validate();

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
}
