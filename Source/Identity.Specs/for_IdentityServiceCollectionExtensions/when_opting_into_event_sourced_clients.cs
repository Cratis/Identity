// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Clients.Secrets;
using Cratis.Identity.Hosting;
using Cratis.Identity.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_opting_into_event_sourced_clients : Specification
{
    IServiceCollection _services;

    void Because()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration["Identity:Clients:EventSourced"] = "true";
        builder.AddCratisIdentity();
        _services = builder.Services;
    }

    [Fact] void should_register_the_fact_writer() => _services.Any(service => service.ImplementationType == typeof(EventSourcedClientRegistrations)).ShouldBeTrue();
    [Fact] void should_remove_the_legacy_writer() => _services.Any(service => service.ImplementationType == typeof(ClientRegistrations)).ShouldBeFalse();
    [Fact] void should_register_the_fact_manager() => _services.Any(service => service.ImplementationType == typeof(ClientApplicationManager<OpenIddictMongoDbApplication>)).ShouldBeTrue();
    [Fact] void should_register_the_read_only_store() => _services.Any(service => service.ImplementationType == typeof(MaterializedApplicationStore<OpenIddictMongoDbApplication>)).ShouldBeTrue();

    [Fact]
    void should_resolve_the_fact_manager_and_read_only_store()
    {
        using var provider = _services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<OpenIddictApplicationManager<OpenIddictMongoDbApplication>>()
            .ShouldBeOfExactType<ClientApplicationManager<OpenIddictMongoDbApplication>>();
        scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationStore<OpenIddictMongoDbApplication>>()
            .ShouldBeOfExactType<MaterializedApplicationStore<OpenIddictMongoDbApplication>>();
    }

    [Fact]
    void should_fail_validation_without_chronicle()
    {
        var error = Catch.Exception(() => _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }));
        error.ShouldBeOfExactType<AggregateException>();
        error.Message.Contains("IEventStore", StringComparison.Ordinal).ShouldBeTrue();
    }
}
