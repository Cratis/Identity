// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Identity.Contracts;
using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.Secrets;

/// <summary>Produces domain-separated, length-framed client-secret comparands.</summary>
/// <param name="keys">The dedicated secret-custody key.</param>
public class ClientSecretDigester(IClientSecretDigestKeys keys)
{
    /// <summary>Computes a keyed digest, never an unkeyed or redeemable secret representation.</summary>
    /// <param name="clientId">The validated client identity.</param>
    /// <param name="secret">The configured secret.</param>
    /// <returns>The revision-prefixed digest.</returns>
    public SecretDigest Digest(ClientId clientId, string secret)
    {
        using var input = new MemoryStream();
        Frame(input, "cratis.identity.client-secret.v1");
        Frame(input, clientId.Value);
        Frame(input, secret);
        var key = keys.Current;
        var hash = Convert.ToBase64String(key.Hash(input.ToArray())).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return new($"v{key.Revision.ToString(CultureInfo.InvariantCulture)}.{hash}");
    }

    /// <summary>Compares only current-revision, canonical keyed digests in constant time.</summary>
    /// <param name="clientId">The validated client identity.</param>
    /// <param name="secret">The presented secret.</param>
    /// <param name="stored">The fact-derived comparand.</param>
    /// <returns>Whether the secret matches.</returns>
    public bool Matches(ClientId clientId, string secret, SecretDigest stored)
    {
        if (string.IsNullOrEmpty(stored.Value))
        {
            return false;
        }

        var expected = Digest(clientId, secret).Value;
        if (stored.Value.Length != expected.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(stored.Value));
    }

    static void Frame(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        stream.Write(length);
        stream.Write(bytes);
    }
}
