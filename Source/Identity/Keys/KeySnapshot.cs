// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Keys;

/// <summary>
/// Represents a <see cref="KeyRing"/> together with the keys that sign and encrypt now.
/// </summary>
/// <param name="Ring">The <see cref="KeyRing"/>.</param>
/// <param name="Signer">The signing key in use.</param>
/// <param name="Encrypter">The encryption key in use.</param>
public record KeySnapshot(KeyRing Ring, RealmKey Signer, RealmKey Encrypter)
{
    /// <summary>
    /// Gets a value indicating whether the keys published, or the keys in use, differ from <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The <see cref="KeySnapshot"/> to compare with.</param>
    /// <returns>True when the options built from the snapshots would differ.</returns>
    public bool DiffersFrom(KeySnapshot other) =>
        Signer.Kid != other.Signer.Kid ||
        Encrypter.Kid != other.Encrypter.Kid ||
        !Kids(Ring.Signing).SequenceEqual(Kids(other.Ring.Signing)) ||
        !Kids(Ring.Encryption).SequenceEqual(Kids(other.Ring.Encryption)) ||
        !DataProtectionIds(Ring).SequenceEqual(DataProtectionIds(other.Ring));

    static IEnumerable<string> DataProtectionIds(KeyRing ring) => ring.DataProtection
        .Select(_ => $"{_.Element.Attribute("id")?.Value}/{_.ActivationDate:O}/{_.ExpirationDate:O}")
        .Order(StringComparer.Ordinal);

    static IEnumerable<string> Kids(IEnumerable<RealmKey> keys) => keys.Select(_ => _.Kid).Order(StringComparer.Ordinal);
}
