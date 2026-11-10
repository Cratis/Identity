// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients;

/// <summary>Server-held HMAC key material; never a client registration value.</summary>
internal sealed class ClientSecretOptions
{
    /// <summary>Gets or sets a base64-encoded key of at least 256 bits from protected configuration.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the positive key revision embedded in every digest.</summary>
    public int Revision { get; set; } = 1;

    /// <inheritdoc/>
    public override string ToString() => "Client secret key [redacted]";
}
