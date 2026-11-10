// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;

namespace Cratis.Identity.Clients.given;

public class a_client_catalog : Specification
{
    protected const string IntrospectionClientId = "introspector";
    protected const string IntrospectionSecret = "spec-only-introspection-canary";
    internal ClientCatalogDriver Clients;

    void Establish()
    {
        var options = Configurations.WithTokenExchange();
        options.IntrospectionClients.Add(new IntrospectionClientOptions { ClientId = IntrospectionClientId, ClientSecret = IntrospectionSecret });
        Clients = ClientCatalogDriver.Create(options);
    }

    async Task Destroy() => await Clients.DisposeAsync();
}
