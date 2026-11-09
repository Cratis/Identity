// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents the RFC 8693 token-exchange configuration. The grant is disabled until a client is configured.
/// </summary>
public class TokenExchangeOptions
{
    /// <summary>
    /// Gets or sets the external issuers whose subject tokens are accepted.
    /// </summary>
    public IList<ExternalIssuerOptions> Issuers { get; set; } = [];

    /// <summary>
    /// Gets or sets the clients enabled for the grant.
    /// </summary>
    public IList<TokenExchangeClientOptions> Clients { get; set; } = [];
}
