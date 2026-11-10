// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation.given;

public class an_oidc_configuration : Specification
{
    protected IdentityOptions options;
    protected OidcClientOptions client;

    void Establish()
    {
        options = Configurations.TwoResourceServers();
        client = new OidcClientOptions
        {
            ClientId = "oidc-web",
            ClientSecret = "unused-in-specs",
            DisplayName = "OIDC web",
            RedirectUris = ["https://rp.example/signin-oidc"],
            PostLogoutRedirectUris = ["https://rp.example/signout-callback-oidc"]
        };
        options.OidcClients.Add(client);
    }

    protected ValidateOptionsResult Validate() => new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = "Development" }).Validate(null, options);
}
