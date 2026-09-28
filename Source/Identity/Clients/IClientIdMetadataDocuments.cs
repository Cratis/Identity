// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients;

/// <summary>
/// Defines the extension point for Client ID Metadata Documents: clients whose <c language="text">client_id</c> is an HTTPS
/// URL pointing at their own metadata, as used by remote MCP clients.
/// </summary>
/// <remarks>
/// Not implemented or registered in v1; only the pre-registered clients in <see cref="ClientDescriptors"/>
/// exist. An implementation must:
/// <list type="bullet">
/// <item><description>fetch only <c language="text">https</c> URLs, refuse redirects, private, loopback and link-local addresses, and cap response size and time (SSRF);</description></item>
/// <item><description>require the document's <c language="text">client_id</c> to equal the URL it was fetched from, and validate every redirect URI;</description></item>
/// <item><description>treat the result as public, native-or-web clients with PKCE, never confidential;</description></item>
/// <item><description>be surfaced to OpenIddict through an application store decorator, so the protocol checks stay OpenIddict's;</description></item>
/// <item><description>never present the client name as proof of identity: a legitimate CIMD URL can be replayed by another program using the same loopback redirect.</description></item>
/// </list>
/// </remarks>
public interface IClientIdMetadataDocuments
{
    /// <summary>
    /// Resolves the metadata document for a URL client identifier.
    /// </summary>
    /// <param name="clientId">The client identifier URL.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The validated <see cref="ClientIdMetadataDocument"/>, or <see langword="null"/> when it cannot be used.</returns>
    Task<ClientIdMetadataDocument?> Resolve(Uri clientId, CancellationToken cancellationToken = default);
}
