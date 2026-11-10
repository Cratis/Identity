// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients.for_ClientCatalog;

public class when_finding_the_cli_client : given.a_client_catalog
{
    OpenIddictApplicationDescriptor _cli;
    bool _ipv4Loopback;
    bool _ipv6Loopback;
    bool _localhost;
    bool _otherHost;
    bool _anySecret;

    async Task Because()
    {
        _cli = await Clients.Find("cratis-cli");
        _ipv4Loopback = await Clients.ValidateRedirect("cratis-cli", "http://127.0.0.1:49152/callback");
        _ipv6Loopback = await Clients.ValidateRedirect("cratis-cli", "http://[::1]:49152/callback");
        _localhost = await Clients.ValidateRedirect("cratis-cli", "http://localhost:49152/callback");
        _otherHost = await Clients.ValidateRedirect("cratis-cli", "http://192.0.2.1:49152/callback");
        _anySecret = await Clients.ValidateSecret("cratis-cli", IntrospectionSecret);
    }

    [Fact] void should_be_a_public_native_client() => (_cli.ClientType, _cli.ApplicationType).ShouldEqual((ClientTypes.Public, ApplicationTypes.Native));
    [Fact] void should_hold_no_secret() => _cli.ClientSecret.ShouldBeNull();
    [Fact] void should_refuse_every_secret() => _anySecret.ShouldBeFalse();
    [Fact] void should_require_pkce() => _cli.Requirements.ShouldContainOnly(Requirements.Features.ProofKeyForCodeExchange);
    [Fact] void should_accept_the_ipv4_loopback_literal_on_any_port() => _ipv4Loopback.ShouldBeTrue();
    [Fact] void should_accept_the_ipv6_loopback_literal_on_any_port() => _ipv6Loopback.ShouldBeTrue();
    [Fact] void should_refuse_localhost() => _localhost.ShouldBeFalse();
    [Fact] void should_refuse_a_redirect_to_another_host() => _otherHost.ShouldBeFalse();
    [Fact] void should_register_only_loopback_literals() => _cli.RedirectUris.Select(_ => _.Host).ShouldContainOnly("127.0.0.1", "[::1]");
    [Fact] void should_use_the_authorization_code_grant() => _cli.Permissions.ShouldContain(Permissions.GrantTypes.AuthorizationCode);
    [Fact] void should_not_allow_client_credentials() => _cli.Permissions.ShouldNotContain(Permissions.GrantTypes.ClientCredentials);
    [Fact] void should_not_allow_the_implicit_grant() => _cli.Permissions.ShouldNotContain(Permissions.GrantTypes.Implicit);
    [Fact] void should_not_allow_the_exchange_grant() => _cli.Permissions.Any(_ => _.Contains("token-exchange", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_allow_introspection() => _cli.Permissions.ShouldNotContain(Permissions.Endpoints.Introspection);
}
