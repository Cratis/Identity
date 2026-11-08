// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_ClientDescriptors;

public class when_declaring_exchange_clients : Specification
{
    OpenIddictApplicationDescriptor[] _all;
    OpenIddictApplicationDescriptor _exchange;
    OpenIddictApplicationDescriptor[] _others;

    void Because()
    {
        var options = Configurations.WithTokenExchange();
        options.IntrospectionClients.Add(new IntrospectionClientOptions { ClientId = "introspector", ClientSecret = "secret" });
        var asOptions = options.AsOptions();
        _all = [.. new ClientDescriptors(new ResourceCatalog(asOptions), asOptions).All()];
        _exchange = _all.Single(_ => _.ClientId == Configurations.ExchangeClientId);
        _others = [.. _all.Where(_ => _.ClientId != Configurations.ExchangeClientId)];
    }

    [Fact] void should_be_confidential() => _exchange.ClientType.ShouldEqual(ClientTypes.Confidential);
    [Fact] void should_carry_the_secret() => _exchange.ClientSecret.ShouldEqual(Configurations.ExchangeClientSecret);
    [Fact] void should_allow_the_token_endpoint() => _exchange.Permissions.ShouldContain(Permissions.Endpoints.Token);
    [Fact] void should_allow_the_exchange_grant() => _exchange.Permissions.ShouldContain(Permissions.Prefixes.GrantType + TokenExchangeConstants.GrantType);
    [Fact] void should_not_allow_the_authorization_endpoint() => _exchange.Permissions.ShouldNotContain(Permissions.Endpoints.Authorization);
    [Fact] void should_not_allow_the_code_grant() => _exchange.Permissions.ShouldNotContain(Permissions.GrantTypes.AuthorizationCode);
    [Fact] void should_not_allow_the_refresh_grant() => _exchange.Permissions.ShouldNotContain(Permissions.GrantTypes.RefreshToken);
    [Fact] void should_not_allow_the_implicit_grant() => _exchange.Permissions.ShouldNotContain(Permissions.GrantTypes.Implicit);
    [Fact] void should_not_allow_client_credentials() => _exchange.Permissions.ShouldNotContain(Permissions.GrantTypes.ClientCredentials);
    [Fact] void should_allow_only_the_ceiling_scope() => _exchange.Permissions.Where(_ => _.StartsWith(Permissions.Prefixes.Scope)).ShouldContainOnly(Permissions.Prefixes.Scope + "direct:read");
    [Fact] void should_allow_the_indicators_of_its_resource() => _exchange.Permissions.Where(_ => _.StartsWith(Permissions.Prefixes.Resource)).ShouldContainOnly(Permissions.Prefixes.Resource + Configurations.DirectMcp);
    [Fact] void should_have_no_redirect_uris() => _exchange.RedirectUris.ShouldBeEmpty();
    [Fact] void should_not_give_any_other_client_the_exchange_grant() => _others.Any(_ => _.Permissions.Contains(Permissions.Prefixes.GrantType + TokenExchangeConstants.GrantType)).ShouldBeFalse();
    [Fact] void should_keep_the_other_clients() => _others.Select(_ => _.ClientId).ShouldContainOnly(ClientDescriptors.CratisCli, "introspector");
}
