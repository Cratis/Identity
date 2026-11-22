// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes;

public class when_querying_unknown_scopes : given.resource_facts
{
    ResourceScope _unknown;
    ResourceScope _differentCase;
    string[] _byNames;
    string[] _listed;
    string[] _unknownResource;

    async Task Because()
    {
        var store = StoreFor(RegisterScope(Resource));
        _unknown = await store.FindByNameAsync("unknown", CancellationToken.None);
        _differentCase = await store.FindByNameAsync("API:read", CancellationToken.None);
        _byNames = await Names(store.FindByNamesAsync(["unknown", "API:read", "api:read"], CancellationToken.None));
        _listed = await Names(store.ListAsync(null, null, CancellationToken.None));
        _unknownResource = await Names(store.FindByResourceAsync("unknown", CancellationToken.None));
    }

    [Fact] void should_return_no_unknown_scope() => _unknown.ShouldBeNull();
    [Fact] void should_use_exact_scope_names() => _differentCase.ShouldBeNull();
    [Fact] void should_omit_unknown_names_from_name_queries() => _byNames.ShouldEqual(["api:read"]);
    [Fact] void should_list_only_known_scopes() => _listed.ShouldEqual(["api:read"]);
    [Fact] void should_return_no_scopes_for_an_unknown_resource() => _unknownResource.ShouldBeEmpty();
}
