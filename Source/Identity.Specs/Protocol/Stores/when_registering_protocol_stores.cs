// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Protocol.Stores;

public class when_registering_protocol_stores : Specification
{
    ServiceProvider _provider;
    IServiceScope _scope;
    object _applications;
    object _scopes;
    object _authorizations;
    object _tokens;
    object _client;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IChronicleClient>());
        var environment = new HostingEnvironment { EnvironmentName = Environments.Development };
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddCratisIdentity(new ConfigurationBuilder().Build(), environment);
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
    }

    async Task Because()
    {
        var services = _scope.ServiceProvider;
        _applications = services.GetRequiredService<IOpenIddictApplicationStore<ClientApplication>>();
        _scopes = services.GetRequiredService<IOpenIddictScopeStore<ResourceScope>>();
        _authorizations = services.GetRequiredService<IOpenIddictAuthorizationStore<OpenIddictMongoDbAuthorization>>();
        _tokens = services.GetRequiredService<IOpenIddictTokenStore<OpenIddictMongoDbToken>>();
        _client = await services.GetRequiredService<IOpenIddictApplicationManager>().FindByClientIdAsync("cratis-cli");
    }

    [Fact] void should_use_the_configuration_entity_in_the_real_manager() => _client.ShouldBeOfExactType<ClientApplication>();
    [Fact] void should_resolve_the_configuration_application_store() => _applications.ShouldBeOfExactType<ConfigurationApplicationStore>();
    [Fact] void should_resolve_the_configuration_scope_store() => _scopes.ShouldBeOfExactType<ConfigurationScopeStore>();
    [Fact] void should_keep_the_mongo_authorization_store() => _authorizations.ShouldBeOfExactType<OpenIddictMongoDbAuthorizationStore<OpenIddictMongoDbAuthorization>>();
    [Fact] void should_use_the_fenced_mongo_token_store() => _tokens.ShouldBeOfExactType<Authorization.FencedTokenStore>();

    async Task Destroy()
    {
        _scope.Dispose();
        await _provider.DisposeAsync();
    }
}
