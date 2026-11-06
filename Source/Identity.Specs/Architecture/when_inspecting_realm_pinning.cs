// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Hosting;
using Cratis.Identity.Realm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Cratis.Identity.Architecture;

public class when_inspecting_realm_pinning : Specification
{
    ServiceProvider _services;
    IEventStoreNamespaceResolver _resolver;

    void Establish()
    {
        var inner = Substitute.For<IEventStoreNamespaceResolver>();
        inner.Resolve().Returns(new EventStoreNamespaceName("tenant-a"));
        var services = new ServiceCollection().AddSingleton(inner);
        services.AddCratisIdentity(new ConfigurationBuilder().Build(), new HostingEnvironment { EnvironmentName = Environments.Development });
        _services = services.BuildServiceProvider();
    }

    void Because() => _resolver = _services.GetRequiredService<IEventStoreNamespaceResolver>();

    [Fact] void should_decorate_the_host_resolver() => _resolver.ShouldBeOfExactType<IdentityRealmNamespaceResolver>();
    [Fact] void should_preserve_the_host_resolver_outside_identity() => _resolver.Resolve().ShouldEqual(new EventStoreNamespaceName("tenant-a"));

    void Destroy() => _services.Dispose();
}
