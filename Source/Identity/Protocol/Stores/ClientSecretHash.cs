// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Cratis.Identity.Protocol.Stores;

/// <summary>Prepares the PBKDF2 envelope understood by OpenIddict's application manager, without a store write.</summary>
internal static class ClientSecretHash
{
    /// <summary>Hashes a declared client secret into the manager-compatible envelope.</summary>
    /// <param name="secret">The declared client secret.</param>
    /// <returns>The obfuscated envelope.</returns>
    internal static string Create(string secret)
    {
        const int iterations = 10_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(secret, salt, iterations, HashAlgorithmName.SHA256, 32);
        var envelope = new byte[13 + salt.Length + key.Length];
        envelope[0] = 1;
        BinaryPrimitives.WriteUInt32BigEndian(envelope.AsSpan(1, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(envelope.AsSpan(5, 4), iterations);
        BinaryPrimitives.WriteUInt32BigEndian(envelope.AsSpan(9, 4), (uint)salt.Length);
        salt.CopyTo(envelope.AsSpan(13));
        key.CopyTo(envelope.AsSpan(13 + salt.Length));
        return Convert.ToBase64String(envelope);
    }
}
