// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_GrantPolicy.given;

public class an_oidc_grant_policy : Specification
{
    private protected GrantPolicy _policy;

    void Establish()
    {
        var options = new IdentityOptions();
        options.OidcClients.Add(new OidcClientOptions { ClientId = "oidc-web", Scopes = ["openid", "profile", "offline_access"] });
        options.OidcClients.Add(new OidcClientOptions { ClientId = "oidc-openid-only", Scopes = ["openid"] });
        _policy = new GrantPolicy(new ResourceCatalog(Configurations.TwoResourceServers().AsOptions()), Options.Create(options));
    }
}
