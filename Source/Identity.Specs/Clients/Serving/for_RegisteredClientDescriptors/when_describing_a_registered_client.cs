// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Contracts.Clients;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients.Serving.for_RegisteredClientDescriptors;

public class when_describing_a_registered_client : given.client_commands
{
    OpenIddictApplicationDescriptor _confidential;
    OpenIddictApplicationDescriptor _native;

    void Because()
    {
        _confidential = RegisteredClientDescriptors.For(_active with
        {
            Configuration = _configuration with { Scopes = ["openid", "mcp"], ResourceIndicators = ["https://app.example/mcp"] }
        });
        _native = RegisteredClientDescriptors.For(_active with
        {
            SecretHash = null,
            Configuration = _configuration with
            {
                ClientType = ClientType.Public,
                ApplicationType = ClientApplicationType.Native,
                RedirectUris = ["http://127.0.0.1/callback"]
            }
        });
    }

    [Fact] void should_carry_the_protocol_client_id() => _confidential.ClientId.ShouldEqual("spec-client");
    [Fact] void should_be_confidential_when_it_can_keep_a_secret() => _confidential.ClientType.ShouldEqual(ClientTypes.Confidential);
    [Fact] void should_never_carry_a_secret() => _confidential.ClientSecret.ShouldBeNull();
    [Fact] void should_allow_the_declared_endpoints() => _confidential.Permissions.ShouldContain(Permissions.Endpoints.Token);
    [Fact] void should_allow_the_declared_grant() => _confidential.Permissions.ShouldContain(Permissions.GrantTypes.AuthorizationCode);
    [Fact] void should_allow_the_declared_scopes() => _confidential.Permissions.ShouldContain(Permissions.Prefixes.Scope + "mcp");
    [Fact] void should_allow_the_declared_resources() => _confidential.Permissions.ShouldContain(Permissions.Prefixes.Resource + "https://app.example/mcp");
    [Fact] void should_not_allow_what_was_not_declared() => _confidential.Permissions.ShouldNotContain(Permissions.Endpoints.Introspection);
    [Fact] void should_require_pkce_when_declared() => _confidential.Requirements.ShouldContain(Requirements.Features.ProofKeyForCodeExchange);
    [Fact] void should_describe_a_public_loopback_client_as_native() => _native.ApplicationType.ShouldEqual(ApplicationTypes.Native);
    [Fact] void should_keep_the_loopback_redirect() => _native.RedirectUris.Select(uri => uri.OriginalString).ShouldContainOnly("http://127.0.0.1/callback");
}
