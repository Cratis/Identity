// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.MetadataDocuments;

namespace Cratis.Identity.Clients;

/// <summary>Resolves the public-client subset of CIMD draft -03 without durable registration.</summary>
/// <remarks>
/// All remote content is validated under the issuer's SSRF, byte, time, cache and admission bounds.
/// Refusals contain no remote content. Configured clients take precedence at the application store.
/// A self-asserted name never proves which local program is using a loopback client identifier.
/// </remarks>
internal interface IClientIdMetadataDocuments
{
    /// <summary>Resolves an exact, canonical client identifier without normalizing it.</summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>A validated, detached descriptor or a reason-only refusal.</returns>
    ValueTask<MetadataDocumentResult> Resolve(ClientId clientId, CancellationToken cancellationToken = default);

    /// <summary>Finds only a live validated reverse mapping, never an arbitrary URL derived from an ID.</summary>
    /// <param name="id">The operational application identifier.</param>
    /// <returns>The client ID, or null.</returns>
    string? ClientIdFor(string id);
}
