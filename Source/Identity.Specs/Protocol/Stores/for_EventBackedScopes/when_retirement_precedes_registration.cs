// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes;

public class when_retirement_precedes_registration : given.resource_facts
{
    ResourceScope _retired;
    string[] _byResource;
    string[] _listed;
    string[] _byNames;

    async Task Because()
    {
        var tombstone = RetireScope(null);
        var resource = Reducer.On(Registered(), tombstone, Context);
        var registered = RegisterScope(resource);
        var repeated = RegisterScope(RetireScope(registered), description: "Replacement");
        var store = StoreFor(repeated);
        _retired = await store.FindByNameAsync("api:read", CancellationToken.None);
        _byResource = await Names(store.FindByResourceAsync("api", CancellationToken.None));
        _listed = await Names(store.ListAsync(null, null, CancellationToken.None));
        _byNames = await Names(store.FindByNamesAsync(["api:read"], CancellationToken.None));
    }

    [Fact] void should_not_resurrect_the_retired_name() => _retired.ShouldBeNull();
    [Fact] void should_not_resurrect_the_resource_mapping() => _byResource.ShouldBeEmpty();
    [Fact] void should_not_list_the_tombstone() => _listed.ShouldBeEmpty();
    [Fact] void should_not_find_the_tombstone_among_names() => _byNames.ShouldBeEmpty();
}
