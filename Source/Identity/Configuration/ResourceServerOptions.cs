// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents the configuration of a resource server tokens can be issued for.
/// </summary>
public class ResourceServerOptions
{
    /// <summary>
    /// Gets or sets the audience written to the <c language="text">aud</c> claim, for example <c language="text">direct-api</c>.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable name shown on the consent page.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the RFC 8707 resource indicators that map to the audience.
    /// </summary>
    public IList<string> Indicators { get; set; } = [];

    /// <summary>
    /// Gets or sets the scopes that belong to the resource server.
    /// </summary>
    public IList<ScopeOptions> Scopes { get; set; } = [];

    /// <summary>
    /// Gets or sets the tenants an account can select when authorizing access to the resource server.
    /// </summary>
    public IList<TenantOptions> Tenants { get; set; } = [];
}
