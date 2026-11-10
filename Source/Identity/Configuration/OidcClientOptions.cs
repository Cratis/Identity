// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents a confidential web client registered for OpenID Connect.
/// </summary>
public class OidcClientOptions
{
    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the client.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exact authorization callback URIs. HTTPS is required except for HTTP IP-loopback literals.
    /// </summary>
    public IList<string> RedirectUris { get; set; } = [];

    /// <summary>
    /// Gets or sets the exact post-logout callback URIs, subject to the same restrictions as authorization callbacks.
    /// </summary>
    public IList<string> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// Gets or sets the maximum scopes this client may be granted, intersected with the requested scopes.
    /// When omitted, defaults to <c language="text">openid profile</c>. An explicit list must not be empty, must include
    /// <c language="text">openid</c> and may also include <c language="text">profile</c> and <c language="text">offline_access</c>.
    /// </summary>
    public IList<string>? Scopes { get; set; }

    /// <summary>
    /// Gets the configured ceiling, using the default only when no list was supplied.
    /// </summary>
    internal IEnumerable<string> EffectiveScopes => Scopes ?? ["openid", "profile"];
}
