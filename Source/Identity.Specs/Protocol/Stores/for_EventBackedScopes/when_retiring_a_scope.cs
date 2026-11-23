// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes;

public class when_retiring_a_scope : given.resource_facts
{
    ResourceScope _retired;
    ResourceScope _byId;
    string[] _byNames;
    string[] _byResource;
    string[] _listed;
    long _count;

    async Task Because()
    {
        var registered = RegisterScope(RegisterScope(Resource), "api:write", "Write API");
        var store = StoreFor(RetireScope(registered));
        _retired = await store.FindByNameAsync("api:read", CancellationToken.None);
        _byId = await store.FindByIdAsync("api:read", CancellationToken.None);
        _byNames = await Names(store.FindByNamesAsync(["api:read", "api:write"], CancellationToken.None));
        _byResource = await Names(store.FindByResourceAsync("api", CancellationToken.None));
        _listed = await Names(store.ListAsync(null, null, CancellationToken.None));
        _count = await store.CountAsync(CancellationToken.None);
    }

    [Fact] void should_hide_the_retired_name() => _retired.ShouldBeNull();
    [Fact] void should_hide_the_retired_identifier() => _byId.ShouldBeNull();
    [Fact] void should_exclude_retired_scopes_from_name_queries() => _byNames.ShouldEqual(["api:write"]);
    [Fact] void should_keep_only_active_scopes_for_the_audience() => _byResource.ShouldEqual(["api:write"]);
    [Fact] void should_list_only_active_scopes() => _listed.ShouldEqual(["api:write"]);
    [Fact] void should_count_only_active_scopes() => _count.ShouldEqual(1);
}
