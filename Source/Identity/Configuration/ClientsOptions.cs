// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>Opt-in Chronicle-backed client configuration.</summary>
public class ClientsOptions
{
    /// <summary>Gets or sets whether clients are reconciled from Chronicle facts.</summary>
    public bool EventSourced { get; set; }

    /// <summary>Gets or sets the base64 secret-custody key, never logged.</summary>
    public string? SecretDigestKey { get; set; }

    /// <summary>Gets or sets the current positive digest-key revision.</summary>
    public int SecretDigestKeyRevision { get; set; } = 1;
}
