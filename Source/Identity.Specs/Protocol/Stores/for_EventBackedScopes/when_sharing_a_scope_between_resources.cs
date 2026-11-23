// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes;

public class when_sharing_a_scope_between_resources : given.two_resources_sharing_a_scope
{
    (string? Name, string? Description, string? DisplayName, string Resources)[] _actual;
    (string? Name, string? Description, string? DisplayName, string Resources)[] _expected;

    async Task Because()
    {
        var options = Options.Create(new IdentityOptions
        {
            Resources =
            [
                new()
                {
                    Audience = "api",
                    DisplayName = "API",
                    Indicators = ["https://api.example"],
                    Scopes = [new() { Name = "api:read", Description = "Read API" }]
                },
                new()
                {
                    Audience = "reports",
                    DisplayName = "Reports",
                    Indicators = ["https://reports.example"],
                    Scopes = [new() { Name = "api:read", Description = "Read reports" }]
                }
            ]
        });
        var resources = new ResourceCatalog(options);
        var configured = new ConfigurationScopeStore(new ConfigurationProtocolCatalog(new ClientDescriptors(resources, options), resources));
        _actual = await Describe(StoreFor(Resource, OtherResource));
        _expected = await Describe(configured);
    }

    [Fact] void should_produce_one_scope_descriptor() => _actual.Length.ShouldEqual(1);
    [Fact] void should_keep_the_shared_scope_name() => _actual[0].Name.ShouldEqual("api:read");
    [Fact] void should_keep_the_first_description() => _actual[0].Description.ShouldEqual("Read API");
    [Fact] void should_include_both_resource_audiences() => _actual[0].Resources.ShouldEqual("api,reports");
    [Fact] void should_match_names_descriptions_display_names_and_audiences() => _actual.ShouldEqual(_expected);

    static async Task<(string? Name, string? Description, string? DisplayName, string Resources)[]> Describe(ConfigurationScopeStore store)
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
