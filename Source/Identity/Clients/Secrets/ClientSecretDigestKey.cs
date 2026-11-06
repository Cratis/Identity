// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Identity.Clients.Secrets;

/// <summary>A custody key for client-secret comparands, never formatted or exposed.</summary>
public sealed class ClientSecretDigestKey
{
    readonly byte[] _key;

    /// <summary>Initializes a new instance of the <see cref="ClientSecretDigestKey"/> class.</summary>
    /// <param name="revision">The positive key revision.</param>
    /// <param name="key">At least 256 bits of key material.</param>
    public ClientSecretDigestKey(int revision, byte[] key)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(revision, 1);
        if (key.Length < 32)
        {
            throw new ArgumentException("Client secret digest keys require at least 256 bits.", nameof(key));
        }

        Revision = revision;
        _key = key.ToArray();
    }

    /// <summary>Gets the revision.</summary>
    public int Revision { get; }

    /// <summary>Hashes framed input without exposing the custody key.</summary>
    /// <param name="input">The domain-separated input.</param>
    /// <returns>The keyed digest.</returns>
    public byte[] Hash(byte[] input) => HMACSHA256.HashData(_key, input);

    /// <inheritdoc/>
    public override string ToString() => $"Client secret digest key revision {Revision} [redacted]";
}
