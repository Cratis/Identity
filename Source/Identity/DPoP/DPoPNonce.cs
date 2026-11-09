// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Identity.DPoP;

/// <summary>Stateless AS-only nonces backed by the issuer's shared Data Protection key ring.</summary>
/// <param name="provider">The existing issuer key ring, including retained rotation keys.</param>
/// <param name="time">The clock used for the overlapping nonce acceptance window.</param>
internal sealed class DPoPNonce(IDataProtectionProvider provider, TimeProvider time)
{
    readonly IDataProtector _protector = provider.CreateProtector("Cratis.Identity.Dpop.Nonce.v1");

    /// <summary>Issues an origin- and key-bound nonce without allocating server state.</summary>
    /// <param name="origin">The configured token endpoint origin.</param>
    /// <param name="jkt">The verified proof key.</param>
    /// <returns>The opaque nonce.</returns>
    internal string Issue(string origin, string jkt) => _protector.Protect(JsonSerializer.Serialize(new Payload("as", origin, jkt, time.GetUtcNow().ToUnixTimeSeconds(), Convert.ToHexString(RandomNumberGenerator.GetBytes(16)))));

    /// <summary>Accepts retained ring keys and timestamps in the current or overlapping previous window.</summary>
    /// <param name="nonce">The supplied nonce.</param>
    /// <param name="origin">The configured origin.</param>
    /// <param name="jkt">The verified proof key.</param>
    /// <returns>Whether all protected fields and time bounds match.</returns>
    internal bool Validate(string? nonce, string origin, string jkt)
    {
        if (string.IsNullOrEmpty(nonce) || nonce.Length > 2048) return false;
        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(_protector.Unprotect(nonce));
            var now = time.GetUtcNow().ToUnixTimeSeconds();
            return payload?.Domain == "as" && payload.Origin == origin && payload.Jkt == jkt && payload.IssuedAt <= now + 30 && payload.IssuedAt > now - 330;
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    sealed record Payload(string Domain, string Origin, string Jkt, long IssuedAt, string Random);
}
