// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes;

public class when_only_resource_registration_exists : given.resource_facts
{
    string[] _listed;
    string[] _byResource;
    string[] _inferred;
    long _count;

    async Task Because()
    {
        var store = StoreFor(Resource);
        _listed = await Names(store.ListAsync(null, null, CancellationToken.None));
        _byResource = await Names(store.FindByResourceAsync("api", CancellationToken.None));
        _inferred = await Names(store.FindByNamesAsync(["api", "api:read", "openid", "offline_access"], CancellationToken.None));
        _count = await store.CountAsync(CancellationToken.None);
    }

    [Fact] void should_not_treat_resource_registration_as_a_scope_grant() => _listed.ShouldBeEmpty();
    [Fact] void should_not_infer_scopes_for_the_audience() => _byResource.ShouldBeEmpty();
    [Fact] void should_not_infer_prefix_or_protocol_scopes() => _inferred.ShouldBeEmpty();
    [Fact] void should_count_no_scopes() => _count.ShouldEqual(0);
}
