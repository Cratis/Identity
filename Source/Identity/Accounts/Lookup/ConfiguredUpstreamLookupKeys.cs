// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>A development-only key source. No production key adapter is implemented.</summary>
public sealed class ConfiguredUpstreamLookupKeys : IUpstreamLookupKeys
{
    static readonly byte[] _developmentKey = RandomNumberGenerator.GetBytes(32);

    /// <summary>Initializes a new instance of the <see cref="ConfiguredUpstreamLookupKeys"/> class.</summary>
    /// <param name="options">The account options.</param>
    /// <param name="environment">The host environment.</param>
    /// <exception cref="AccountOperationRefused">The experiment is not enabled in Development or the key is invalid.</exception>
    public ConfiguredUpstreamLookupKeys(IOptions<IdentityOptions> options, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() || !options.Value.Accounts.EventSourced)
        {
            throw new AccountOperationRefused("pending owner decision D18");
        }

        var accounts = options.Value.Accounts;
        try
        {
            var bytes = string.IsNullOrEmpty(accounts.LookupKey) ? _developmentKey : Convert.FromBase64String(accounts.LookupKey);
            Current = new(accounts.LookupKeyRevision, bytes);
        }
        catch (FormatException)
        {
            throw new AccountOperationRefused("invalid_lookup_key_configuration");
        }
    }

    /// <inheritdoc/>
    public UpstreamLookupKey Current { get; }
}
