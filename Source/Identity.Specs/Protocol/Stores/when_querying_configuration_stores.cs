// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores;

public class when_querying_configuration_stores : given.a_configuration_catalog
{
    readonly List<ClientApplication> _page = [];
    readonly List<string> _query = [];
    readonly List<ResourceScope> _scopePage = [];
    readonly List<ResourceScope> _byResource = [];
    readonly List<ResourceScope> _byNames = [];
    readonly List<string> _scopeQuery = [];
    ClientApplication _unknownApplication;
    ResourceScope _unknownScope;
    long _clientCount;
    long _scopeCount;
    string _client;
    string _scope;
    string[] _descriptions;
    bool _sameId;

    async Task Because()
    {
        _unknownApplication = await Applications.FindByClientIdAsync("SPEC-API", CancellationToken.None);
        _unknownScope = await Scopes.FindByNameAsync("unknown", CancellationToken.None);
        _clientCount = await Applications.CountAsync(_ => _.Where(application => application.ClientId == "spec-api"), CancellationToken.None);
        _scopeCount = await Scopes.CountAsync(_ => _.Select(scope => scope.Name), CancellationToken.None);
        _client = await Applications.GetAsync((query, name) => query.Where(_ => _.ClientId == name).Select(_ => _.ClientId), "spec-api", CancellationToken.None);
        _scope = await Scopes.GetAsync((query, name) => query.Where(_ => _.Name == name).Select(_ => _.Name), "spec:read", CancellationToken.None);
        await foreach (var application in Applications.ListAsync(1, 1, CancellationToken.None)) _page.Add(application);
        await foreach (var name in Applications.ListAsync((query, prefix) => query.Where(_ => _.ClientId.StartsWith(prefix)).Select(_ => _.ClientId), "spec", CancellationToken.None)) _query.Add(name);
        await foreach (var scope in Scopes.ListAsync(1, 1, CancellationToken.None)) _scopePage.Add(scope);
        await foreach (var scope in Scopes.FindByResourceAsync("spec-api", CancellationToken.None)) _byResource.Add(scope);
        await foreach (var scope in Scopes.FindByNamesAsync(["spec:read", "unknown"], CancellationToken.None)) _byNames.Add(scope);
        await foreach (var name in Scopes.ListAsync((query, prefix) => query.Where(_ => _.Name.StartsWith(prefix)).Select(_ => _.Name), "spec", CancellationToken.None)) _scopeQuery.Add(name);
        List<string> descriptions = [];
        foreach (var scope in _byResource) descriptions.Add(await Scopes.GetDescriptionAsync(scope, CancellationToken.None));
        _descriptions = [.. descriptions];
        var applicationById = await Applications.FindByIdAsync(await Applications.GetIdAsync(_page.Single(), CancellationToken.None), CancellationToken.None);
        var scopeById = await Scopes.FindByIdAsync(await Scopes.GetIdAsync(_scopePage.Single(), CancellationToken.None), CancellationToken.None);
        _sameId = ReferenceEquals(applicationById, _page.Single()) && ReferenceEquals(scopeById, _scopePage.Single());
    }

    [Fact] void should_not_match_clients_case_insensitively() => _unknownApplication.ShouldBeNull();
    [Fact] void should_return_null_for_an_unknown_scope() => _unknownScope.ShouldBeNull();
    [Fact] void should_count_matching_clients() => _clientCount.ShouldEqual(1);
    [Fact] void should_count_scopes() => _scopeCount.ShouldEqual(2);
    [Fact] void should_get_a_projected_client() => _client.ShouldEqual("spec-api");
    [Fact] void should_get_a_projected_scope() => _scope.ShouldEqual("spec:read");
    [Fact] void should_page_clients() => _page.Select(_ => _.ClientId).ShouldEqual(["spec-api"]);
    [Fact] void should_query_clients() => _query.ShouldEqual(["spec-api"]);
    [Fact] void should_page_scopes() => _scopePage.Select(_ => _.Name).ShouldEqual(["spec:write"]);
    [Fact] void should_find_scopes_by_resource() => _byResource.Select(_ => _.Name).ShouldEqual(["spec:read", "spec:write"]);
    [Fact] void should_find_only_known_scope_names() => _byNames.Select(_ => _.Name).ShouldEqual(["spec:read"]);
    [Fact] void should_query_scopes() => _scopeQuery.ShouldEqual(["spec:read", "spec:write"]);
    [Fact] void should_preserve_scope_descriptions() => _descriptions.ShouldEqual(["Read", "Write"]);
    [Fact] void should_round_trip_identifiers() => _sameId.ShouldBeTrue();
}
