// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Resources.for_ResourceCatalog;

public class when_listing_the_scopes : given.a_resource_catalog
{
    (string Name, string Description)[] _scopes;

    async Task Because() => _scopes = [.. (await Resources.Scopes()).Select(_ => (_.Name, _.Description ?? string.Empty))];

    [Fact] void should_list_the_scopes_of_every_resource_with_descriptions() => _scopes.ShouldContainOnly(("direct:read", "Read"), ("direct:content.write", "Write content"), ("direct:work", "Start work"), ("studio:read", "Read"));
}
