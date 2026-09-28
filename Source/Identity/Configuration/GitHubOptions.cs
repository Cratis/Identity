// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents the configuration of the GitHub OAuth app used as upstream login.
/// </summary>
public class GitHubOptions
{
    /// <summary>
    /// Gets or sets the GitHub OAuth app client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub OAuth app client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
}
