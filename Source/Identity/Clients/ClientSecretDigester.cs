// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Identity.Contracts.Clients;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Clients;

/// <summary>Produces keyed client secret digests without retaining plaintext.</summary>
internal sealed class ClientSecretDigester
{
    readonly byte[] _key;
    readonly int _revision;

    /// <summary>Initializes a new instance of the <see cref="ClientSecretDigester"/> class.</summary>
    /// <param name="options">The server-held key, independent of any command.</param>
    /// <exception cref="InvalidOperationException">The key is absent or invalid.</exception>
    public ClientSecretDigester(IOptions<ClientSecretOptions> options)
    {
        try
        {
            _key = Convert.FromBase64String(options.Value.Key);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("invalid_client_secret_key_configuration");
        }

        _revision = options.Value.Revision;
        if (_key.Length < 32 || _revision < 1)
        {
            throw new InvalidOperationException("invalid_client_secret_key_configuration");
        }
    }

    /// <summary>Hashes plaintext using the configured HMAC-SHA-256 key.</summary>
    /// <param name="secret">The command-only plaintext.</param>
    /// <returns>The non-redeemable digest in the D2 contract form.</returns>
    public SecretDigest Digest(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes($"cratis.identity.client-secret.v1\0{secret}");
        try
        {
            return new($"v{_revision}.{WebEncoders.Base64UrlEncode(HMACSHA256.HashData(_key, bytes))}");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    /// <inheritdoc/>
    public override string ToString() => "Client secret digester [redacted]";
}
