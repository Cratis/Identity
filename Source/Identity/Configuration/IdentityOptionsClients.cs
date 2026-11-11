// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>Issuer-owned client registration policy.</summary>
public class IdentityOptionsClients
{
    /// <summary>Gets or sets the opt-in public Client ID Metadata Document policy.</summary>
    public ClientIdMetadataOptions MetadataDocuments { get; set; } = new();
}
