// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores;

public class when_queries_are_hostile : given.a_configuration_catalog
{
    readonly List<Exception> _canceled = [];
    readonly List<Exception> _negative = [];
    readonly List<ClientApplication> _empty = [];
    ClientApplication _unknownClient;
    ResourceScope _unknownScope;
    string _unknownQuery;

    async Task Because()
    {
        _unknownClient = await Applications.FindByIdAsync("unknown", CancellationToken.None);
        _unknownScope = await Scopes.FindByIdAsync("unknown", CancellationToken.None);
        _unknownQuery = await Applications.GetAsync((query, _) => query.Where(application => application.ClientId == "unknown").Select(application => application.ClientId), 0, CancellationToken.None);
        await foreach (var application in Applications.ListAsync(0, 0, CancellationToken.None)) _empty.Add(application);
        var canceled = new CancellationToken(true);
        _canceled.Add(await Catch.Exception(async () => await Applications.FindByClientIdAsync("spec-api", canceled)));
        _canceled.Add(await Catch.Exception(async () => await Scopes.FindByNameAsync("spec:read", canceled)));
        _canceled.Add(await Catch.Exception(async () =>
        {
            await foreach (var scope in Scopes.FindByResourceAsync("unknown", canceled)) { }
        }));
        _negative.Add(Catch.Exception(() => Applications.ListAsync(-1, 0, CancellationToken.None)));
        _negative.Add(Catch.Exception(() => Scopes.ListAsync(null, -1, CancellationToken.None)));
    }

    [Fact] void should_not_resolve_an_unknown_application_identifier() => _unknownClient.ShouldBeNull();
    [Fact] void should_not_resolve_an_unknown_scope_identifier() => _unknownScope.ShouldBeNull();
    [Fact] void should_return_null_for_an_empty_projected_query() => _unknownQuery.ShouldBeNull();
    [Fact] void should_honor_a_zero_count() => _empty.ShouldBeEmpty();
    [Fact] void should_honor_cancellation_even_for_empty_results() => _canceled.TrueForAll(_ => _ is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_reject_negative_page_bounds() => _negative.TrueForAll(_ => _ is ArgumentOutOfRangeException).ShouldBeTrue();
}
