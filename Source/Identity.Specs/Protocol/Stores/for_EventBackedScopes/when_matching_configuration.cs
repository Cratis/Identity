// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes;

public class when_matching_configuration : given.resource_facts
{
    (string?, string?, string?, string)[] _actual;
    (string?, string?, string?, string)[] _expected;

    async Task Because()
    {
        var options = Options.Create(new IdentityOptions
        {
            Resources = [new()
            {
                Audience = "api",
                DisplayName = "API",
                Indicators = ["https://api.example"],
                Scopes = [new() { Name = "api:read", Description = "Read API" }, new() { Name = "api:write", Description = "Write API" }]
            }]
        });
        var resources = new ResourceCatalog(options);
        var configured = new ConfigurationScopeStore(new ConfigurationProtocolCatalog(new ClientDescriptors(resources, options), resources));
        var registered = RegisterScope(RegisterScope(Resource), "api:write", "Write API");
        _actual = await Describe(StoreFor(registered));
        _expected = await Describe(configured);
    }

    [Fact] void should_produce_both_explicit_scope_descriptors() => _actual.Length.ShouldEqual(2);
    [Fact] void should_match_names_descriptions_display_names_and_audiences() => _actual.ShouldEqual(_expected);

    static async Task<(string?, string?, string?, string)[]> Describe(ConfigurationScopeStore store)
    {
        List<(string?, string?, string?, string)> descriptions = [];
        await foreach (var scope in store.ListAsync(null, null, CancellationToken.None))
        {
            descriptions.Add((
                await store.GetNameAsync(scope, CancellationToken.None),
                await store.GetDescriptionAsync(scope, CancellationToken.None),
                await store.GetDisplayNameAsync(scope, CancellationToken.None),
                string.Join(',', (await store.GetResourcesAsync(scope, CancellationToken.None)).Order(StringComparer.Ordinal))));
        }
        return descriptions.ToArray();
    }
}
