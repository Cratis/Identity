// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients.for_ClientCatalog;

public class when_listing_the_clients : given.a_client_catalog
{
    string[] _ids;
    OpenIddictApplicationDescriptor _introspection;
    OpenIddictApplicationDescriptor _exchange;
    bool _correctSecret;
    bool _wrongSecret;

    async Task Because()
    {
        _ids = await Clients.ClientIds();
        _introspection = await Clients.Find(IntrospectionClientId);
        _exchange = await Clients.Find(Configurations.ExchangeClientId);
        _correctSecret = await Clients.ValidateSecret(IntrospectionClientId, IntrospectionSecret);
        _wrongSecret = await Clients.ValidateSecret(IntrospectionClientId, "wrong-spec-secret");
    }

    [Fact] void should_list_exactly_the_declared_clients() => _ids.ShouldContainOnly("cratis-cli", IntrospectionClientId, Configurations.ExchangeClientId);
    [Fact] void should_make_the_introspection_client_confidential() => _introspection.ClientType.ShouldEqual(ClientTypes.Confidential);
    [Fact] void should_allow_the_introspection_client_to_introspect_only() => _introspection.Permissions.ShouldContainOnly(Permissions.Endpoints.Introspection);
    [Fact] void should_not_infer_scopes_for_the_introspection_client() => _introspection.Permissions.Any(_ => _.StartsWith(Permissions.Prefixes.Scope, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_authenticate_the_introspection_secret() => _correctSecret.ShouldBeTrue();
    [Fact] void should_reject_a_wrong_secret() => _wrongSecret.ShouldBeFalse();
    [Fact] void should_not_expose_the_clear_secret() => _introspection.ClientSecret.ShouldNotEqual(IntrospectionSecret);
    [Fact] void should_make_the_exchange_client_confidential() => _exchange.ClientType.ShouldEqual(ClientTypes.Confidential);
    [Fact]
    void should_give_the_exchange_client_only_the_token_endpoint_and_the_exchange_grant() =>
        _exchange.Permissions.Where(_ => !_.StartsWith(Permissions.Prefixes.Scope, StringComparison.Ordinal) && !_.StartsWith(Permissions.Prefixes.Resource, StringComparison.Ordinal))
            .ShouldContainOnly(Permissions.Endpoints.Token, Permissions.Prefixes.GrantType + TokenExchangeConstants.GrantType);
    [Fact] void should_limit_the_exchange_client_to_the_resource_of_its_ceiling() => _exchange.Permissions.Where(_ => _.StartsWith(Permissions.Prefixes.Resource, StringComparison.Ordinal)).ShouldContainOnly(Permissions.Prefixes.Resource + Configurations.DirectMcp);
    [Fact] void should_limit_the_exchange_client_to_its_scope_ceiling() => _exchange.Permissions.Where(_ => _.StartsWith(Permissions.Prefixes.Scope, StringComparison.Ordinal)).ShouldContainOnly(Permissions.Prefixes.Scope + "direct:read");
}
