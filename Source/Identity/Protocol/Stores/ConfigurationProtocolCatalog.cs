// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Resources;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Protocol.Stores;

/// <summary>Snapshots the configured descriptors for the read-only protocol stores.</summary>
/// <param name="clients">The declared client descriptors.</param>
/// <param name="resources">The configured resource catalog.</param>
internal sealed class ConfigurationProtocolCatalog(ClientDescriptors clients, IResourceCatalog resources) : IProtocolCatalog
{
    /// <inheritdoc/>
    public IReadOnlyList<ClientApplication> Applications { get; } = clients.All().Select(_ => new ClientApplication(_)).ToList().AsReadOnly();

    /// <inheritdoc/>
    public IReadOnlyList<ResourceScope> Scopes { get; } = resources.All
        .SelectMany(resource => resource.Scopes.Select(scope => (resource, scope)))
        .GroupBy(_ => _.scope.Name, StringComparer.Ordinal)
        .Select(group =>
        {
            var descriptor = new OpenIddictScopeDescriptor { Name = group.Key, Description = group.First().scope.Description };
            descriptor.Resources.UnionWith(group.Select(_ => _.resource.Audience));
            return new ResourceScope(descriptor);
        }).ToList().AsReadOnly();
}
