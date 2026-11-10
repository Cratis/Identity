// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;

namespace Cratis.Identity.Resources.given;

public class a_resource_catalog : Specification
{
    internal ResourceCatalogDriver Resources;

    void Establish() => Resources = ResourceCatalogDriver.Create(Configurations.TwoResourceServers());

    async Task Destroy() => await Resources.DisposeAsync();
}
