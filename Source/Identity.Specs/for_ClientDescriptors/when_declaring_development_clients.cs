// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_ClientDescriptors;

public class when_declaring_development_clients : Specification
{
    ClientDescriptors _descriptors;
    OpenIddictApplicationDescriptor _client;
    OpenIddictApplicationDescriptor _cli;

    void Establish()
    {
        var options = Configurations.TwoResourceServers();
        var development = new DevelopmentOptions
        {
            Clients =
            [
                new DevelopmentClientOptions
                {
                    ClientId = "local-claude",
                    DisplayName = "Claude (local)",
                    RedirectUris = ["http://localhost/callback", "http://127.0.0.1/callback"]
                }
            ]
        };
        _descriptors = new ClientDescriptors(new ResourceCatalog(options.AsOptions()), options.AsOptions(), development);
    }

    void Because()
    {
        _client = _descriptors.All().Single(_ => _.ClientId == "local-claude");
        _cli = _descriptors.CratisCliDescriptor();
    }

    [Fact] void should_register_the_client() => _client.ShouldNotBeNull();
    [Fact] void should_be_a_public_native_client() => (_client.ClientType, _client.ApplicationType).ShouldEqual((OpenIddictConstants.ClientTypes.Public, OpenIddictConstants.ApplicationTypes.Native));
    [Fact] void should_require_explicit_consent() => _client.ConsentType.ShouldEqual(OpenIddictConstants.ConsentTypes.Explicit);
    [Fact] void should_require_pkce() => _client.Requirements.ShouldContain(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
    [Fact] void should_allow_localhost() => _client.RedirectUris.Select(_ => _.Host).ShouldContain("localhost");
    [Fact] void should_not_allow_the_device_grant() => _client.Permissions.ShouldNotContain(OpenIddictConstants.Permissions.GrantTypes.DeviceCode);
    [Fact] void should_not_permit_an_identity_token() => _client.Permissions.ShouldNotContain(OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId);
    [Fact] void should_leave_the_cli_without_localhost() => _cli.RedirectUris.Select(_ => _.Host).ShouldNotContain("localhost");
}
