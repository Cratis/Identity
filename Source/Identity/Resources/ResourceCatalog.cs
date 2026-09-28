// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Resources;

/// <summary>
/// Represents an implementation of <see cref="IResourceCatalog"/> backed by configuration.
/// </summary>
/// <param name="options">The <see cref="IdentityOptions"/>.</param>
public class ResourceCatalog(IOptions<IdentityOptions> options) : IResourceCatalog
{
    readonly ResourceServer[] _resources = [.. options.Value.Resources.Select(ToResourceServer)];

    /// <inheritdoc/>
    public IEnumerable<ResourceServer> All => _resources;

    /// <inheritdoc/>
    public ResourceServer? FindByIndicator(string indicator)
    {
        var normalized = ResourceIndicator.Normalize(indicator);
        return normalized is null ? null : _resources.FirstOrDefault(_ => _.Indicators.Contains(normalized, StringComparer.Ordinal));
    }

    /// <inheritdoc/>
    public ResourceServer? FindByAudience(string audience) =>
        _resources.FirstOrDefault(_ => string.Equals(_.Audience, audience, StringComparison.Ordinal));

    /// <inheritdoc/>
    public IEnumerable<ResourceServer> FindByScope(string scope) => _resources.Where(_ => _.Owns(scope));

    static ResourceServer ToResourceServer(ResourceServerOptions resource) => new(
        resource.Audience,
        string.IsNullOrWhiteSpace(resource.DisplayName) ? resource.Audience : resource.DisplayName,
        [.. resource.Indicators.Select(ResourceIndicator.Normalize).OfType<string>()],
        [.. resource.Scopes.Select(_ => new ScopeDefinition(_.Name, _.Description))]);
}
