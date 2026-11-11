// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_OidcClientDescriptors.given;

public class an_oidc_client : Specification
{
    protected OidcClientOptions client;
    protected OpenIddictApplicationDescriptor descriptor;
    protected OpenIddictApplicationDescriptor[] all;
    IdentityOptions _options;

    void Establish()
    {
        _options = Configurations.TwoResourceServers();
        client = new OidcClientOptions
        {
            ClientId = "oidc-web",
            ClientSecret = "unused-in-specs",
            DisplayName = "OIDC web",
            RedirectUris = ["https://rp.example/signin-oidc"],
            PostLogoutRedirectUris = ["https://rp.example/signout-callback-oidc"]
        };
        _options.OidcClients.Add(client);
    }

    protected void Declare()
    {
        var options = _options.AsOptions();
        all = [.. new ClientDescriptors(new ResourceCatalog(options), options).All()];
        descriptor = all.Single(_ => _.ClientId == client.ClientId);
    }
}
