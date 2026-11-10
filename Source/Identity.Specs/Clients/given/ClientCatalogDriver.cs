// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Protocol.Stores;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Clients.given;

/// <summary>Reads clients the way the protocol does, through the application manager, whatever source backs it.</summary>
internal sealed class ClientCatalogDriver : IAsyncDisposable
{
    readonly ServiceProvider _provider;
    readonly IServiceScope _scope;
    readonly IOpenIddictApplicationManager _manager;
    readonly IOpenIddictApplicationStore<ClientApplication> _store;

    ClientCatalogDriver(ServiceProvider provider)
    {
        _provider = provider;
        _scope = provider.CreateScope();
        _manager = _scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        _store = _scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationStore<ClientApplication>>();
    }

    /// <summary>Creates a driver over the clients the options declare.</summary>
    /// <param name="options">The configured realm policy.</param>
    /// <returns>The driver; the caller disposes it.</returns>
    public static ClientCatalogDriver Create(IdentityOptions options) => new(CatalogServices.Build(options));

    /// <summary>Finds a client by its exact client id.</summary>
    /// <param name="clientId">The client id.</param>
    /// <returns>The client as a descriptor, or <see langword="null"/> when no such client is known.</returns>
    public async Task<OpenIddictApplicationDescriptor?> Find(string clientId)
    {
        var application = await _manager.FindByClientIdAsync(clientId);
        if (application is null)
        {
            return null;
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _manager.PopulateAsync(descriptor, application);
        return descriptor;
    }

    /// <summary>Lists the client ids of every known client.</summary>
    /// <returns>The client ids.</returns>
    public async Task<string[]> ClientIds()
    {
        var ids = new List<string>();
        await foreach (var application in _store.ListAsync(null, null, CancellationToken.None))
        {
            ids.Add(await _store.GetClientIdAsync(application, CancellationToken.None));
        }

        return [.. ids];
    }

    /// <summary>Validates a client secret the way the token endpoint does.</summary>
    /// <param name="clientId">The client id.</param>
    /// <param name="secret">The presented secret.</param>
    /// <returns><see langword="true"/> when the client exists and the secret matches.</returns>
    public async Task<bool> ValidateSecret(string clientId, string secret)
    {
        var application = await _manager.FindByClientIdAsync(clientId);
        return application is not null && await _manager.ValidateClientSecretAsync(application, secret);
    }

    /// <summary>Validates a redirect URI the way the authorization endpoint does.</summary>
    /// <param name="clientId">The client id.</param>
    /// <param name="redirectUri">The presented redirect URI.</param>
    /// <returns><see langword="true"/> when the client exists and accepts the redirect URI.</returns>
    public async Task<bool> ValidateRedirect(string clientId, string redirectUri)
    {
        var application = await _manager.FindByClientIdAsync(clientId);
        return application is not null && await _manager.ValidateRedirectUriAsync(application, redirectUri);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _provider.DisposeAsync();
    }
}
