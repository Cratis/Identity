// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using OpenIddict.Server;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Advertises only the actually supported public-client CIMD profile.</summary>
/// <param name="options">Issuer-owned opt-in policy.</param>
internal sealed class AdvertiseClientIdMetadataDocuments(ClientIdMetadataOptions options) : IOpenIddictServerHandler<OpenIddictServerEvents.HandleConfigurationRequestContext>
{
    /// <inheritdoc/>
    public ValueTask HandleAsync(OpenIddictServerEvents.HandleConfigurationRequestContext context)
    {
        if (options.Enabled)
        {
            context.Metadata["client_id_metadata_document_supported"] = true;
            context.TokenEndpointAuthenticationMethods.Add("none");
        }

        return ValueTask.CompletedTask;
    }
}
