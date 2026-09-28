// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents a configured tenant of a resource server.
/// </summary>
public class TenantOptions
{
    /// <summary>
    /// Gets or sets the tenant identifier written to the <c language="text">tid</c> claim.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable tenant name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric GitHub user identifiers that may select the tenant.
    /// </summary>
    /// <remarks>
    /// Numeric identifiers are used rather than logins because a GitHub login can be renamed and then claimed
    /// by someone else.
    /// </remarks>
    public IList<string> Members { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether any signed-in account may select the tenant. Defaults to
    /// <see langword="false"/>, so an unlisted account is refused.
    /// </summary>
    public bool AllowAllAccounts { get; set; }
}
