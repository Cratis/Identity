// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Identity.Clients;

/// <summary>Server-held HMAC key material; never a client registration value.</summary>
internal sealed class ClientSecretOptions
{
    /// <summary>Gets or sets a base64-encoded key of at least 256 bits from protected configuration.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the positive key revision embedded in every digest.</summary>
    public int Revision { get; set; } = 1;

    /// <summary>Gets or sets the maximum old/new secret overlap; defaults to seven days.</summary>
    public TimeSpan MaxOverlap { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Checks server key syntax and strength without exposing or retaining validation copies.</summary>
    /// <returns>Whether the configured key contains at least 256 bits.</returns>
    public bool HasValidKey()
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(Key);
            var valid = bytes.Length >= 32;
            CryptographicOperations.ZeroMemory(bytes);
            return valid;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public override string ToString() => "Client secret key [redacted]";
}
