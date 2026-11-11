// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Fetches and validates one metadata revision without caching or retaining refused content.</summary>
internal interface IMetadataDocumentFetcher
{
    /// <summary>Fetches the exact client identifier.</summary>
    /// <param name="clientId">The metadata URL.</param>
    /// <param name="cancellationToken">The caller deadline.</param>
    /// <returns>The validated snapshot or a reason-only refusal.</returns>
    ValueTask<MetadataDocumentResult> FetchAsync(ClientId clientId, CancellationToken cancellationToken);
}
