// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.Upstream;

/// <summary>
/// Represents which authentication scheme performs upstream login. Configured in code, not from configuration.
/// </summary>
public class UpstreamLoginOptions
{
    /// <summary>
    /// Gets or sets the authentication scheme challenged when the user has no Identity session.
    /// </summary>
    public string Scheme { get; set; } = IdentitySchemes.GitHub;
}
