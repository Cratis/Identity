// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_PublicApi;

public class when_exporting_types : Specification
{
    Type[] _exported;

    void Because() => _exported = typeof(IdentityHostApplicationBuilderExtensions).Assembly.GetExportedTypes();

    [Fact]
    void should_only_export_hosting_entry_points_and_host_configurable_options()
    {
        Type[] allowed =
        [
            typeof(IdentityHostApplicationBuilderExtensions),
            typeof(IdentityWebApplicationExtensions),
            typeof(AccountsOptions),
            typeof(DeviceGrantOptions),
            typeof(DPoPOptions),
            typeof(ExternalIssuerOptions),
            typeof(GitHubOptions),
            typeof(IdentityEndpointOptions),
            typeof(IdentityOptions),
            typeof(IdentityOptionsClients),
            typeof(ClientIdMetadataOptions),
            typeof(IntrospectionClientOptions),
            typeof(MongoDBOptions),
            typeof(OidcClientOptions),
            typeof(OidcOptions),
            typeof(ResourceServerOptions),
            typeof(ScopeOptions),
            typeof(SecretStoreOptions),
            typeof(TenantOptions),
            typeof(TenantsCallbackOptions),
            typeof(TokenExchangeClientOptions),
            typeof(TokenExchangeOptions),
            typeof(TokenOptions),
            typeof(TokenRateLimitOptions),
            typeof(UpstreamLoginOptions),

            // Emitted by the Arc source generator, not an Identity public API.
            _exported.Single(type => type.FullName == "Cratis.Arc.Generated.GeneratedMarker")
        ];

        _exported.Except(allowed).ShouldBeEmpty();
        allowed.Except(_exported).ShouldBeEmpty();
    }
}
