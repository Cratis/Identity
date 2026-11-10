// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Resources;

/// <summary>Registers the configured resource catalog.</summary>
internal static class ResourceServiceCollectionExtensions
{
    /// <summary>Adds the resource catalog.</summary>
    /// <param name="services">The services.</param>
    internal static void AddResources(this IServiceCollection services) => services.AddSingleton<IResourceCatalog, ResourceCatalog>();
}
