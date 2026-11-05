// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents the secret store Identity reads its realm keys and its Data Protection key ring from, bound from
/// the <c language="text">Identity:SecretStore</c> configuration section.
/// </summary>
public class SecretStoreOptions
{
    /// <summary>
    /// Gets or sets the directory the platform's secret store mounts, for example a Kubernetes Secret volume or a
    /// CSI secrets-store mount. It holds <c language="text">signing.&lt;name&gt;.crt</c>/<c language="text">.key</c>,
    /// <c language="text">encryption.&lt;name&gt;.crt</c>/<c language="text">.key</c> and
    /// <c language="text">data-protection.&lt;name&gt;.xml</c> files.
    /// </summary>
    /// <remarks>
    /// Required outside development.
    /// </remarks>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how often the directory is read again, so a published or retired key takes effect without a restart.
    /// </summary>
    public TimeSpan ReloadInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets a value indicating whether a secret store is configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Path);
}
