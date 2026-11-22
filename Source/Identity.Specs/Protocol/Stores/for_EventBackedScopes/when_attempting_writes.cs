// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes;

public class when_attempting_writes : given.resource_facts
{
    readonly List<Exception> _errors = [];
    string[] _remaining;

    async Task Because()
    {
        var store = StoreFor(RegisterScope(Resource));
        var scope = await store.FindByNameAsync("api:read", CancellationToken.None);
        Func<ValueTask>[] writes =
        [
            () => store.CreateAsync(scope, CancellationToken.None),
            () => store.UpdateAsync(scope, CancellationToken.None),
            () => store.DeleteAsync(scope, CancellationToken.None),
            async () => await store.InstantiateAsync(CancellationToken.None),
            () => store.SetNameAsync(scope, "replacement", CancellationToken.None),
            () => store.SetDescriptionAsync(scope, "Replacement", CancellationToken.None),
            () => store.SetDescriptionsAsync(scope, [], CancellationToken.None),
            () => store.SetDisplayNameAsync(scope, "Replacement", CancellationToken.None),
            () => store.SetDisplayNamesAsync(scope, [], CancellationToken.None),
            () => store.SetPropertiesAsync(scope, [], CancellationToken.None),
            () => store.SetResourcesAsync(scope, ["replacement"], CancellationToken.None)
        ];
        foreach (var write in writes) _errors.Add(await Catch.Exception(async () => await write()));
        _remaining = await Names(store.ListAsync(null, null, CancellationToken.None));
    }

    [Fact] void should_exercise_every_mutator() => _errors.Count.ShouldEqual(11);
    [Fact] void should_refuse_every_write() => _errors.TrueForAll(_ => _ is NotSupportedException).ShouldBeTrue();
    [Fact] void should_preserve_the_scope_rows() => _remaining.ShouldEqual(["api:read"]);
}
