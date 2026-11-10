// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Resources;

/// <summary>Reads the server's configured resource reservations.</summary>
/// <param name="catalog">The trusted configured catalogue.</param>
internal sealed class ResourceRegistrationReads(IResourceCatalog catalog)
{
    /// <summary>Captures configured resources for an owner command decision.</summary>
    /// <returns>The trusted snapshot.</returns>
    public ResourceRegistrationDecision Read() => new(catalog.All.ToArray());
}
