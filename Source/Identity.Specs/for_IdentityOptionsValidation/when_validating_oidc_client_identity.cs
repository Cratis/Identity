// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.for_IdentityOptionsValidation;

[PerFactSpecificationLifecycle]
public class when_validating_oidc_client_identity : given.an_oidc_configuration
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    void should_reject_an_empty_id(string id)
    {
        client.ClientId = id;
        Validate().Failed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    void should_reject_an_empty_secret(string secret)
    {
        client.ClientSecret = secret;
        Validate().Failed.ShouldBeTrue();
    }

    [Fact]
    void should_reject_duplicate_oidc_ids()
    {
        options.OidcClients.Add(new OidcClientOptions { ClientId = client.ClientId, ClientSecret = "unused-in-specs" });
        Validate().Failed.ShouldBeTrue();
    }

    [Fact]
    void should_reject_the_cli_id()
    {
        client.ClientId = Clients.ClientDescriptors.CratisCli;
        Validate().Failed.ShouldBeTrue();
    }

    [Fact]
    void should_reject_an_introspection_client_id()
    {
        options.IntrospectionClients.Add(new IntrospectionClientOptions { ClientId = client.ClientId, ClientSecret = "unused-in-specs" });
        Validate().Failed.ShouldBeTrue();
    }

    [Fact]
    void should_reject_an_exchange_client_id()
    {
        options.TokenExchange.Clients.Add(new TokenExchangeClientOptions { ClientId = client.ClientId, ClientSecret = "unused-in-specs" });
        OidcClientOptionsValidation.Validate(options).ShouldNotBeEmpty();
    }
}
