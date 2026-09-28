// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents the configuration of a scope.
/// </summary>
public class ScopeOptions
{
    /// <summary>
    /// Gets or sets the scope name, for example <c language="text">direct:read</c>.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description shown on the consent page.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
