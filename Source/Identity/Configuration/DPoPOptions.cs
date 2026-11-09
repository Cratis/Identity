// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>Configures opt-in RFC 9449 token-endpoint sender binding.</summary>
public class DPoPOptions
{
    /// <summary>Gets or sets whether DPoP issuance is enabled. Disabled by default.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets whether every client must use DPoP for new grants.</summary>
    public bool RequireForAllClients { get; set; }

    /// <summary>Gets or sets the registered clients that must use DPoP.</summary>
    public IList<string> RequiredClients { get; set; } = [];

    /// <summary>Gets or sets the exact public token endpoint, never inferred from request headers.</summary>
    public string PublicTokenEndpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the profile's fixed five-minute proof lifetime.</summary>
    public TimeSpan ProofLifetime { get; set; } = TimeSpan.FromSeconds(300);

    /// <summary>Gets or sets the profile's fixed thirty-second future skew.</summary>
    public TimeSpan FutureSkew { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the profile's fixed shared replay reservation lifetime.</summary>
    public TimeSpan ReplayTtl { get; set; } = TimeSpan.FromSeconds(390);
}
