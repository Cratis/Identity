// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Keys;

/// <summary>
/// Represents an immutable, validated snapshot of everything the secret store holds.
/// </summary>
/// <param name="Signing">The signing keys.</param>
/// <param name="Encryption">The encryption keys.</param>
/// <param name="DataProtection">The Data Protection keys.</param>
public record KeyRing(IReadOnlyList<RealmKey> Signing, IReadOnlyList<RealmKey> Encryption, IReadOnlyList<DataProtectionKey> DataProtection);
