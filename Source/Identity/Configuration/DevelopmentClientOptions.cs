// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents a pre-registered public local client of the development profile.
/// </summary>
internal class DevelopmentClientOptions
{
    /// <summary>
    /// Gets or sets the client identifier, for example <c language="text">local-claude</c>.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name shown on the consent page.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exact loopback redirect URIs. A port is accepted at request time, as for every native client.
    /// </summary>
    public IList<string> RedirectUris { get; set; } = [];
}
