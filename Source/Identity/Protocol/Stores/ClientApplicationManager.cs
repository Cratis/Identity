// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Clients;
using Cratis.Identity.Clients.Serving;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;

namespace Cratis.Identity.Protocol.Stores;

/// <summary>
/// The application manager, which verifies the secret of a client registered while Cratis Identity runs against the digests
/// its registration holds. A client the configuration declares is verified the way OpenIddict does.
/// </summary>
/// <typeparam name="TApplication">The application type.</typeparam>
/// <param name="cache">The application cache.</param>
/// <param name="logger">The logger.</param>
/// <param name="options">The core options.</param>
/// <param name="store">The application store.</param>
/// <param name="services">Resolves the digester only when a registered client presents a secret, so a deployment that registers no confidential client needs no digest key.</param>
internal sealed class ClientApplicationManager<TApplication>(
    IOpenIddictApplicationCache<TApplication> cache,
    ILogger<OpenIddictApplicationManager<TApplication>> logger,
    IOptionsMonitor<OpenIddictCoreOptions> options,
    IOpenIddictApplicationStore<TApplication> store,
    IServiceProvider services) : OpenIddictApplicationManager<TApplication>(cache, logger, options, store)
    where TApplication : class
{
    /// <inheritdoc/>
    public override ValueTask<bool> ValidateClientSecretAsync(TApplication application, string secret, CancellationToken cancellationToken = default)
    {
        if (application is not ClientApplication { Registration: { } registration })
        {
            return base.ValidateClientSecretAsync(application, secret, cancellationToken);
        }

        var digester = services.GetService(typeof(ClientSecretDigester)) as ClientSecretDigester;
        return ValueTask.FromResult(digester is not null && RegisteredClientSecrets.IsValid(registration, secret, digester, ((services.GetService(typeof(TimeProvider)) as TimeProvider) ?? TimeProvider.System).GetUtcNow()));
    }
}
