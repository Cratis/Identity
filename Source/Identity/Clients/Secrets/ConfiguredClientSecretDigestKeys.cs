// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Clients.Secrets;

/// <summary>Reads a dedicated custody key, with an ephemeral per-process Development fallback.</summary>
public sealed class ConfiguredClientSecretDigestKeys : IClientSecretDigestKeys
{
    static readonly byte[] _developmentKey = RandomNumberGenerator.GetBytes(32);

    /// <summary>Initializes a new instance of the <see cref="ConfiguredClientSecretDigestKeys"/> class.</summary>
    /// <param name="options">The client options.</param>
    /// <param name="environment">The host environment.</param>
    public ConfiguredClientSecretDigestKeys(IOptions<IdentityOptions> options, IHostEnvironment environment)
    {
        var clients = options.Value.Clients;
        var key = clients.SecretDigestKey;
        if (string.IsNullOrEmpty(key))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("A client secret digest key is required outside Development.");
            }

            Current = new(clients.SecretDigestKeyRevision, _developmentKey);
            return;
        }

        try
        {
            Current = new(clients.SecretDigestKeyRevision, Convert.FromBase64String(key));
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Invalid client secret digest key configuration.");
        }
    }

    /// <inheritdoc/>
    public ClientSecretDigestKey Current { get; }
}
