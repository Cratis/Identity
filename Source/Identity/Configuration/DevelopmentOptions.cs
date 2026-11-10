// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents the opt-in local-development profile, bound from the <c language="text">Identity:Development</c> configuration section.
/// </summary>
/// <remarks>
/// The profile is refused outside Development. It is internal so the shipped public configuration surface does not grow:
/// the section is read by the profile itself, never bound onto <see cref="IdentityOptions"/> by configuration.
/// </remarks>
internal class DevelopmentOptions
{
    /// <summary>
    /// The name of the section, below <see cref="IdentityOptions.SectionName"/>.
    /// </summary>
    public const string SectionName = "Development";

    /// <summary>
    /// Gets or sets a value indicating whether the profile is active. Off by default; startup refuses it outside Development.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the pre-registered public local clients.
    /// </summary>
    public IList<DevelopmentClientOptions> Clients { get; set; } = [];

    /// <summary>
    /// Gets or sets the local resource servers. They replace, and are never merged into, the configured resources.
    /// </summary>
    public IList<ResourceServerOptions> Resources { get; set; } = [];
}
