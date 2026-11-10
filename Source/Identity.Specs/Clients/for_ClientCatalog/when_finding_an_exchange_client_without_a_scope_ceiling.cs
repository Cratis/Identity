// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.given;
using Cratis.Identity.given;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients.for_ClientCatalog;

public class when_finding_an_exchange_client_without_a_scope_ceiling : Specification
{
    ClientCatalogDriver _clients;
    OpenIddictApplicationDescriptor _exchange;

    async Task Because()
    {
        var options = Configurations.WithTokenExchange();
        options.TokenExchange.Clients[0].Scopes = [];
        _clients = ClientCatalogDriver.Create(options);
        _exchange = await _clients.Find(Configurations.ExchangeClientId);
    }

    async Task Destroy() => await _clients.DisposeAsync();

    [Fact]
    void should_allow_every_scope_of_its_resources_and_no_other() =>
        _exchange.Permissions.Where(_ => _.StartsWith(Permissions.Prefixes.Scope, StringComparison.Ordinal)).ShouldContainOnly(
            Permissions.Prefixes.Scope + "direct:read",
            Permissions.Prefixes.Scope + "direct:content.write",
            Permissions.Prefixes.Scope + "direct:work");
}
