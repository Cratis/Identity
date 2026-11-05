// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts;
using Cratis.Identity.Contracts.Clients;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;

namespace Cratis.Identity.Clients.Secrets;

/// <summary>Authenticates confidential clients only against the current fact-derived comparand.</summary>
/// <typeparam name="TApplication">The application entity.</typeparam>
/// <param name="cache">The stock application cache.</param>
/// <param name="logger">The stock manager logger.</param>
/// <param name="options">The stock core options.</param>
/// <param name="store">The stock application store.</param>
/// <param name="digester">The dedicated keyed comparand verifier.</param>
public class ClientApplicationManager<TApplication>(
    IOpenIddictApplicationCache<TApplication> cache,
    ILogger<OpenIddictApplicationManager<TApplication>> logger,
    IOptionsMonitor<OpenIddictCoreOptions> options,
    IOpenIddictApplicationStore<TApplication> store,
    ClientSecretDigester digester)
    : OpenIddictApplicationManager<TApplication>(cache, logger, options, store)
    where TApplication : class
{
    /// <inheritdoc/>
    public override async ValueTask<bool> ValidateClientSecretAsync(TApplication application, string secret, CancellationToken cancellationToken = default)
    {
        if (!await HasClientTypeAsync(application, OpenIddictConstants.ClientTypes.Confidential, cancellationToken))
        {
            return false;
        }

        var clientId = await Store.GetClientIdAsync(application, cancellationToken);
        var comparand = await Store.GetClientSecretAsync(application, cancellationToken);
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(comparand) || string.IsNullOrEmpty(secret))
        {
            return false;
        }

        try
        {
            return digester.Matches(ClientId.Create(clientId), secret, new SecretDigest(comparand));
        }
        catch (InvalidClientValue)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    protected override ValueTask<string> ObfuscateClientSecretAsync(string secret, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("client configuration is written by Chronicle facts");
}
