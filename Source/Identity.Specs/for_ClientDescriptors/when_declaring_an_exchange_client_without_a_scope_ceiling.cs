// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_ClientDescriptors;

public class when_declaring_an_exchange_client_without_a_scope_ceiling : Specification
{
    OpenIddictApplicationDescriptor _exchange;

    void Because()
    {
        var options = Configurations.WithTokenExchange();
        options.TokenExchange.Clients[0].Scopes = [];
        var asOptions = options.AsOptions();
        _exchange = new ClientDescriptors(new ResourceCatalog(asOptions), asOptions).All().Single(_ => _.ClientId == Configurations.ExchangeClientId);
    }

    [Fact]
    void should_allow_every_scope_of_its_resources_and_no_other() =>
        _exchange.Permissions.Where(_ => _.StartsWith(Permissions.Prefixes.Scope)).ShouldContainOnly(
            Permissions.Prefixes.Scope + "direct:read",
            Permissions.Prefixes.Scope + "direct:content.write",
            Permissions.Prefixes.Scope + "direct:work");
}
