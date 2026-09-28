// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents a PKCS#12 certificate loaded from disk.
/// </summary>
public class CertificateOptions
{
    /// <summary>
    /// Gets or sets the path to the PKCS#12 (<c language="text">.pfx</c>) file.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password protecting the file.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether a certificate is configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Path);
}
