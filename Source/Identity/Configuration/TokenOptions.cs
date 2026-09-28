// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents token lifetime configuration.
/// </summary>
public class TokenOptions
{
    /// <summary>
    /// Gets or sets the lifetime of access tokens. Access tokens are self-contained JWTs, so this is also the
    /// longest a revoked grant can still be used against a resource server that does not introspect.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the lifetime of authorization codes.
    /// </summary>
    public TimeSpan AuthorizationCodeLifetime { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the absolute lifetime of a refresh-token family. Rotation does not extend it: every
    /// rotated refresh token keeps the expiry of the grant it descends from.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}
