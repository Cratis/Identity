// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents the product-owned selectable-tenants callback. The wire contract is internal until contract v1 locks.
/// </summary>
public class TenantsCallbackOptions
{
    /// <summary>
    /// Gets or sets the absolute callback URL. HTTPS is required outside Development.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bearer secret, supplied through a secret store or environment. Outside Development it must have at least 32 characters.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deadline for the entire callback, including its response body. Defaults to three seconds; at most ten seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);
}
