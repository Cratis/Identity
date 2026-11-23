// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes;

public class when_scope_registration_precedes_the_resource : given.resource_facts
{
    string[] _before;
    string[] _after;
    string[] _emptyHistory;

    async Task Because()
    {
        var orphan = RegisterScope(null);
        _before = await Names(StoreFor(orphan).ListAsync(null, null, CancellationToken.None));
        var registered = Reducer.On(Registered(), orphan, Context);
        _after = await Names(StoreFor(registered).FindByResourceAsync("api", CancellationToken.None));
        _emptyHistory = await Names(StoreFor().ListAsync(null, null, CancellationToken.None));
    }

    [Fact] void should_hide_scopes_until_the_resource_is_registered() => _before.ShouldBeEmpty();
    [Fact] void should_use_the_explicit_audience_after_resource_registration() => _after.ShouldEqual(["api:read"]);
    [Fact] void should_serve_no_scopes_without_resource_facts() => _emptyHistory.ShouldBeEmpty();
}
