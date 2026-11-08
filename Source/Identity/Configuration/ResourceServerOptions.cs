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
    /// Gets or sets the single RFC 8707 resource indicator that maps to the audience.
    /// Separate API and MCP surfaces require separate resource entries.
    /// </summary>
    public IList<string> Indicators { get; set; } = [];

    /// <summary>
    /// Gets or sets the scopes that belong to the resource server.
    /// </summary>
    public IList<ScopeOptions> Scopes { get; set; } = [];

    /// <summary>
    /// Gets or sets the tenants used as Development fixtures only; refused outside Development.
    /// </summary>
    public IList<TenantOptions> Tenants { get; set; } = [];

    /// <summary>
    /// Gets or sets the product-owned selectable-tenants callback.
    /// </summary>
    public TenantsCallbackOptions? TenantsCallback { get; set; }
}
