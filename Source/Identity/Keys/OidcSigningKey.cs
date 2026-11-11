// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace Cratis.Identity.Keys;

/// <summary>Selects only a currently valid, prepublished RS256 signer. OAuth's selector is unchanged.</summary>
internal static class OidcSigningKey
{
    /// <summary>Selects by the same most-recent activation and ordinal kid order as the realm key ring.</summary>
    /// <param name="keys">The published signing-key ring.</param>
    /// <param name="now">The current UTC time.</param>
    /// <returns>The eligible RSA key, without a not-yet-eligible fallback.</returns>
    public static RealmKey? Select(IEnumerable<RealmKey> keys, DateTimeOffset now) => keys
        .Where(key => IsRsaSigner(key) && key.NotBefore + KeyEligibility.PrePublication <= now && key.NotAfter > now)
        .OrderByDescending(key => key.NotBefore)
        .ThenBy(key => key.Kid, StringComparer.Ordinal)
        .FirstOrDefault();

    /// <summary>Finds an eligible credential already included in this request's published server snapshot.</summary>
    /// <param name="server">The request's server options.</param>
    /// <param name="keys">The deployed ring, or null for development ephemeral credentials.</param>
    /// <param name="now">The current UTC time.</param>
    /// <returns>An RS256 credential, or null when issuance must be refused.</returns>
    public static SigningCredentials? Find(OpenIddictServerOptions server, SecretStoreKeys? keys, DateTimeOffset now)
    {
        if (keys is null)
        {
            return server.SigningCredentials.FirstOrDefault(credential => credential.Algorithm == SecurityAlgorithms.RsaSha256 && credential.Key.KeySize >= 2048);
        }
        return keys.FindIdentityTokenCredential(server.SigningCredentials, now);
    }

    /// <summary>Rejects deployment with OIDC clients but no eligible RSA signer.</summary>
    /// <param name="keys">The validated secret-store snapshot.</param>
    /// <param name="now">The current UTC time.</param>
    /// <exception cref="OptionsValidationException">No eligible RSA signing key is available.</exception>
    public static void Require(SecretStoreKeys keys, DateTimeOffset now)
    {
        if (Select(keys.Current.Ring.Signing, now) is null)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(IdentityOptions), ["OpenID Connect clients require a currently valid, prepublished RSA signing key of at least 2048 bits for RS256."]);
        }
    }

    static bool IsRsaSigner(RealmKey key)
    {
        if (key.Purpose != KeyPurpose.Signing || key.Algorithm != SecurityAlgorithms.RsaSha256) return false;
        using var rsa = key.Certificate.GetRSAPublicKey();
        return rsa?.KeySize >= 2048;
    }
}
