// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents the issuance gate for OpenID Connect identity-only grants.
/// </summary>
public class OidcOptions
{
    /// <summary>
    /// Gets or sets whether identity-only issuance is enabled. Disabled by default.
    /// </summary>
    public bool IssuanceEnabled { get; set; }

    /// <summary>
    /// Gets or sets the recorded consent decision: <c>ImplicitFencedPath</c> or <c>ActivationBarrier</c>.
    /// Issuance cannot be enabled without one of these decisions.
    /// </summary>
    public string ConsentDecision { get; set; } = string.Empty;
}
