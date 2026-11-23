// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Protocol.Stores;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Resources;

/// <summary>Builds scope snapshots from reduced Internal resource facts, never authorization grants.</summary>
internal static class EventBackedScopes
{
    /// <summary>Maps registered resources' active scopes to protocol rows without selecting a production store.</summary>
    /// <param name="resources">Resource states folded by <see cref="ResourceRegistrationReducer"/>.</param>
    /// <returns>A snapshot containing only explicitly registered, non-retired scopes.</returns>
    public static IReadOnlyList<ResourceScope> From(IEnumerable<ResourceRegistration> resources) => resources
        .Where(resource => !string.IsNullOrEmpty(resource.ResourceIndicator))
        .SelectMany(resource => resource.Scopes.Where(scope => !scope.Retired).Select(scope => (resource, scope)))
        .GroupBy(_ => _.scope.ScopeName, StringComparer.Ordinal)
        .Select(group =>
        {
            var descriptor = new OpenIddictScopeDescriptor { Name = group.Key, Description = group.First().scope.Description.Value };
            descriptor.Resources.UnionWith(group.Select(_ => _.resource.Audience));
            return new ResourceScope(descriptor);
        }).ToList().AsReadOnly();
}
