// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Cratis.Identity.Operational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using MongoDB.Driver;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_resolving_the_revision_guard : Specification
{
    ServiceProvider _provider;
    LookupRevisionGuard _guard;
    IMongoCollection<LookupRevision> _collection;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddOptions<IdentityOptions>();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = Environments.Development });
        services.AddSingleton(new MongoClient("mongodb://localhost:27017").GetDatabase("identity_revision_guard_specs"));
        services.AddAccountServices();
        _provider = services.BuildServiceProvider();
    }

    void Because()
    {
        _guard = _provider.GetServices<IHostedService>().OfType<LookupRevisionGuard>().Single();
        _collection = _provider.GetRequiredService<IMongoCollection<LookupRevision>>();
    }

    [Fact] void should_resolve_the_hosted_guard() => _guard.ShouldNotBeNull();
    [Fact] void should_use_the_registered_identity_database() => _collection.CollectionNamespace.DatabaseNamespace.DatabaseName.ShouldEqual("identity_revision_guard_specs");
    [Fact] void should_use_a_dedicated_metadata_collection() => _collection.CollectionNamespace.CollectionName.ShouldEqual("account_lookup_revisions");

    void Destroy() => _provider.Dispose();
}
