// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_a_client_references_a_resource_disabled_by_hosted_configuration : given.a_token_exchange_configuration
{
    string _audience;

    void Establish() => _audience = Client.Resources[0];

    void Because()
    {
        HostedSharedConfiguration.Remove(options, [_audience]);
        Validate();
    }

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
    [Fact] void should_name_the_client_and_the_resource() => result.FailureMessage.ShouldContain($"\"{Client.ClientId}\" references a resource \"{_audience}\" that is not configured or was disabled");
    [Fact] void should_not_include_the_client_secret() => result.FailureMessage.ShouldNotContain(Client.ClientSecret);
}
