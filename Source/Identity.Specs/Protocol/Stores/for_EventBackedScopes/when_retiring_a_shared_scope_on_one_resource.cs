// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes;

public class when_retiring_a_shared_scope_on_one_resource : given.two_resources_sharing_a_scope
{
    ResourceScope _scope;
    string[] _audiences;
    string? _description;
    string[] _byRetiredResource;
    string[] _byActiveResource;
    string[] _listed;
    long _count;

    async Task Because()
    {
        var store = StoreFor(RetireScope(Resource), OtherResource);
        _scope = await store.FindByNameAsync("api:read", CancellationToken.None);
        _audiences = (await store.GetResourcesAsync(_scope, CancellationToken.None)).ToArray();
        _description = await store.GetDescriptionAsync(_scope, CancellationToken.None);
        _byRetiredResource = await Names(store.FindByResourceAsync("api", CancellationToken.None));
        _byActiveResource = await Names(store.FindByResourceAsync("reports", CancellationToken.None));
        _listed = await Names(store.ListAsync(null, null, CancellationToken.None));
        _count = await store.CountAsync(CancellationToken.None);
    }

    [Fact] void should_keep_the_shared_scope() => _scope.ShouldNotBeNull();
    [Fact] void should_keep_only_the_active_resource_audience() => _audiences.ShouldEqual(["reports"]);
    [Fact] void should_use_the_active_resources_description() => _description.ShouldEqual("Read reports");
    [Fact] void should_exclude_the_retired_resource_mapping() => _byRetiredResource.ShouldBeEmpty();
    [Fact] void should_keep_the_active_resource_mapping() => _byActiveResource.ShouldEqual(["api:read"]);
    [Fact] void should_list_the_shared_scope() => _listed.ShouldEqual(["api:read"]);
    [Fact] void should_count_one_scope_descriptor() => _count.ShouldEqual(1);
}
