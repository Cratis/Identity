// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>One development lookup key; its secret is never formatted.</summary>
public sealed class UpstreamLookupKey
{
    readonly byte[] _key;

    /// <summary>Initializes a new instance of the <see cref="UpstreamLookupKey"/> class.</summary>
    /// <param name="revision">The positive revision.</param>
    /// <param name="key">At least 256 bits of key material.</param>
    /// <exception cref="AccountOperationRefused">Revision or key material is invalid.</exception>
    public UpstreamLookupKey(int revision, byte[] key)
    {
        if (revision < 1 || key.Length < 32)
        {
            throw new AccountOperationRefused("invalid_lookup_key_configuration");
        }

        Revision = revision;
        _key = key.ToArray();
        Check = Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes("cratis.identity.upstream-key-check.v1")));
    }

    /// <summary>Gets the revision.</summary>
    public int Revision { get; }

    /// <summary>Gets a domain-separated check, not the key.</summary>
    public string Check { get; }

    /// <summary>Hashes input without exposing the secret.</summary>
    /// <param name="input">The framed input.</param>
    /// <returns>The keyed digest.</returns>
    public byte[] Hash(byte[] input) => HMACSHA256.HashData(_key, input);

    /// <inheritdoc/>
    public override string ToString() => $"Lookup key revision {Revision} [redacted]";
}
