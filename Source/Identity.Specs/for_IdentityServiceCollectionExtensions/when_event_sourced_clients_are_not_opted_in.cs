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
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_event_sourced_clients_are_not_opted_in : Specification
{
    IServiceCollection _services;

    void Because()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.AddCratisIdentity();
        _services = builder.Services;
    }

    [Fact] void should_keep_the_legacy_writer() => _services.Any(service => service.ImplementationType == typeof(ClientRegistrations)).ShouldBeTrue();
    [Fact] void should_not_register_the_fact_writer() => _services.Any(service => service.ImplementationType == typeof(EventSourcedClientRegistrations)).ShouldBeFalse();
    [Fact] void should_keep_the_stock_manager() => _services.Any(service => service.ImplementationType == typeof(OpenIddictApplicationManager<>)).ShouldBeTrue();
    [Fact]
    void should_keep_the_stock_store()
    {
        using var provider = _services.BuildServiceProvider();
        provider.GetRequiredService<IOpenIddictApplicationStore<OpenIddictMongoDbApplication>>()
            .ShouldBeOfExactType<OpenIddictMongoDbApplicationStore>();
    }
    [Fact] void should_not_register_the_materialized_store() => _services.Any(service => service.ImplementationType == typeof(MaterializedApplicationStore<OpenIddictMongoDbApplication>)).ShouldBeFalse();
    [Fact] void should_not_register_the_fact_manager() => _services.Any(service => service.ImplementationType == typeof(ClientApplicationManager<OpenIddictMongoDbApplication>)).ShouldBeFalse();
}
