// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Resources;

/// <summary>
/// Defines the catalog of resource servers tokens can be issued for.
/// </summary>
public interface IResourceCatalog
{
    /// <summary>
    /// Gets all resource servers.
    /// </summary>
    IEnumerable<ResourceServer> All { get; }

    /// <summary>
    /// Finds the resource server a resource indicator maps to.
    /// </summary>
    /// <param name="indicator">The RFC 8707 resource indicator.</param>
    /// <returns>The resource server, or <see langword="null"/> if the indicator is unknown.</returns>
    ResourceServer? FindByIndicator(string indicator);

    /// <summary>
    /// Finds the resource server with a given audience.
    /// </summary>
    /// <param name="audience">The audience.</param>
    /// <returns>The resource server, or <see langword="null"/> if the audience is unknown.</returns>
    ResourceServer? FindByAudience(string audience);

    /// <summary>
    /// Finds the resource servers that own a scope.
    /// </summary>
    /// <param name="scope">The scope name.</param>
    /// <returns>The resource servers that own the scope.</returns>
    IEnumerable<ResourceServer> FindByScope(string scope);
}
