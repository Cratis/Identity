// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Identity.Keys;

/// <summary>
/// Represents one realm key loaded from the secret store.
/// </summary>
/// <param name="Name">The name from the file name, for example <c language="text">2026a</c>.</param>
/// <param name="Kid">The key id: the certificate thumbprint, as OpenIddict uses it. Deterministic, so it is stable across reloads and never reused for other material.</param>
/// <param name="Certificate">The <see cref="X509Certificate2"/> including its private key.</param>
/// <param name="Purpose">The <see cref="KeyPurpose"/>.</param>
/// <param name="Algorithm">The JOSE algorithm the key is used with.</param>
internal record RealmKey(string Name, string Kid, X509Certificate2 Certificate, KeyPurpose Purpose, string Algorithm)
{
    /// <summary>
    /// Gets the start of the certificate's validity.
    /// </summary>
    public DateTimeOffset NotBefore { get; } = new(Certificate.NotBefore);

    /// <summary>
    /// Gets the end of the certificate's validity.
    /// </summary>
    public DateTimeOffset NotAfter { get; } = new(Certificate.NotAfter);
}
