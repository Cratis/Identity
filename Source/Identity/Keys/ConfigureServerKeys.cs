// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace Cratis.Identity.Keys;

/// <summary>
/// Puts the realm keys of the secret store into the <see cref="OpenIddictServerOptions"/>.
/// </summary>
/// <remarks>
/// OpenIddict signs with, and encrypts with, the first credential of its list, publishes every signing credential
/// through JWKS and decrypts with every encryption credential. Its own post-configuration sorts the list by
/// certificate validity, so <see cref="PostConfigure"/> runs after it and moves the key that
/// <see cref="KeyEligibility"/> selected to the front.
/// </remarks>
/// <param name="keys">The <see cref="SecretStoreKeys"/>.</param>
internal class ConfigureServerKeys(SecretStoreKeys keys) : IConfigureOptions<OpenIddictServerOptions>, IPostConfigureOptions<OpenIddictServerOptions>
{
    /// <inheritdoc/>
    public void Configure(OpenIddictServerOptions options)
    {
        var ring = keys.Current.Ring;
        foreach (var key in ring.Signing)
        {
            options.SigningCredentials.Add(new SigningCredentials(SecurityKeyFor(key), key.Algorithm));
        }

        foreach (var key in ring.Encryption)
        {
            options.EncryptionCredentials.Add(new EncryptingCredentials(new X509SecurityKey(key.Certificate), key.Algorithm, SecurityAlgorithms.Aes256CbcHmacSha512));
        }
    }

    /// <inheritdoc/>
    public void PostConfigure(string? name, OpenIddictServerOptions options)
    {
        var snapshot = keys.Current;
        MoveToFront(options.SigningCredentials, snapshot.Signer.Kid, _ => _.Key.KeyId);
        MoveToFront(options.EncryptionCredentials, snapshot.Encrypter.Kid, _ => _.Key.KeyId);
    }

    /// <summary>
    /// X509SecurityKey cannot sign ES256 or publish an EC JWK correctly, so EC keys use an ECDsaSecurityKey with the same kid.
    /// </summary>
    /// <param name="key">The <see cref="RealmKey"/>.</param>
    /// <returns>The <see cref="SecurityKey"/>.</returns>
    static SecurityKey SecurityKeyFor(RealmKey key) => key.Algorithm == SecurityAlgorithms.EcdsaSha256
        ? new ECDsaSecurityKey(System.Security.Cryptography.X509Certificates.ECDsaCertificateExtensions.GetECDsaPrivateKey(key.Certificate)) { KeyId = key.Kid }
        : new X509SecurityKey(key.Certificate);

    static void MoveToFront<T>(List<T> credentials, string kid, Func<T, string> kidOf)
    {
        var index = credentials.FindIndex(_ => string.Equals(kidOf(_), kid, StringComparison.OrdinalIgnoreCase));
        if (index <= 0)
        {
            return;
        }

        var current = credentials[index];
        credentials.RemoveAt(index);
        credentials.Insert(0, current);
    }
}
