// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Identity.Realm.for_IdentityRealmNamespaceResolver;

public class when_resolving_outside_the_realm_scope : Specification
{
    EventStoreNamespaceName _namespace;

    void Because()
    {
        var inner = Substitute.For<IEventStoreNamespaceResolver>();
        inner.Resolve().Returns(new EventStoreNamespaceName("tenant-a"));
        _namespace = new IdentityRealmNamespaceResolver(inner).Resolve();
    }

    [Fact] void should_keep_host_namespace_resolution() => _namespace.ShouldEqual(new EventStoreNamespaceName("tenant-a"));
}
