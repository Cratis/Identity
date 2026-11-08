// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents a confidential client that may use the RFC 8693 token-exchange grant and nothing else.
/// </summary>
public class TokenExchangeClientOptions
{
    /// <summary>
    /// Gets or sets the client identifier. It must be unique across all clients.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret, supplied through a secret store or environment. Outside Development it must have at least 32 characters.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exact <see cref="ExternalIssuerOptions.Issuer"/> values whose subject tokens this client may present.
    /// </summary>
    public IList<string> Issuers { get; set; } = [];

    /// <summary>
    /// Gets or sets the audiences of the resource servers this client may request tokens for.
    /// </summary>
    public IList<string> Resources { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional scope ceiling. When empty, every scope of each allowed resource is allowed.
    /// </summary>
    public IList<string> Scopes { get; set; } = [];
}
