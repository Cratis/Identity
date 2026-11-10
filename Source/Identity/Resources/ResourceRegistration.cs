// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Resources;

namespace Cratis.Identity.Resources;

/// <summary>Internal resource state for guarded owner decisions; never an authorization catalogue.</summary>
/// <param name="Id">The resource stream identity.</param>
internal sealed record ResourceRegistration(ResourceId Id)
{
    /// <summary>Gets the normalized resource indicator; empty until registration.</summary>
    public string ResourceIndicator { get; init; } = string.Empty;

    /// <summary>Gets the permanently reserved audience.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Gets the resource-local scope namespace.</summary>
    public string ScopePrefix { get; init; } = string.Empty;

    /// <summary>Gets the Internal non-person display label.</summary>
    public ResourceDisplayName DisplayName { get; init; } = new(string.Empty);

    /// <summary>Gets all scopes, retaining retired entries.</summary>
    public IReadOnlyList<ResourceScopeRegistration> Scopes { get; init; } = [];
}
