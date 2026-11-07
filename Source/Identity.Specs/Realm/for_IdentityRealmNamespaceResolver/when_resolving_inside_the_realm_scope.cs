// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Identity.Realm.for_IdentityRealmNamespaceResolver;

public class when_resolving_inside_the_realm_scope : Specification
{
    EventStoreNamespaceName _namespace;

    void Because()
    {
        var inner = Substitute.For<IEventStoreNamespaceResolver>();
        inner.Resolve().Returns(new EventStoreNamespaceName("tenant-a"));
        using var scope = IdentityRealmScope.Begin();
        _namespace = new IdentityRealmNamespaceResolver(inner).Resolve();
    }

    [Fact] void should_pin_the_realm_namespace() => _namespace.ShouldEqual(EventStoreNamespaceName.Default);
    [Fact] void should_restore_the_previous_scope() => IdentityRealmScope.IsActive.ShouldBeFalse();
}
