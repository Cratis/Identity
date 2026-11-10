// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.given;
using Cratis.Identity.Configuration;
using Cratis.Identity.Protocol.Stores;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Resources.given;

/// <summary>Reads resources and scopes the way the protocol does, whatever source backs them.</summary>
internal sealed class ResourceCatalogDriver : IAsyncDisposable
{
    readonly ServiceProvider _provider;
    readonly IServiceScope _scope;
    readonly IResourceCatalog _catalog;
    readonly IOpenIddictScopeStore<ResourceScope> _scopes;

    ResourceCatalogDriver(ServiceProvider provider)
    {
        _provider = provider;
        _scope = provider.CreateScope();
        _catalog = provider.GetRequiredService<IResourceCatalog>();
        _scopes = _scope.ServiceProvider.GetRequiredService<IOpenIddictScopeStore<ResourceScope>>();
    }

    /// <summary>Creates a driver over the resources the options declare.</summary>
    /// <param name="options">The configured realm policy.</param>
    /// <returns>The driver; the caller disposes it.</returns>
    public static ResourceCatalogDriver Create(IdentityOptions options) => new(CatalogServices.Build(options));

    /// <summary>Finds the resource server an RFC 8707 resource indicator maps to.</summary>
    /// <param name="indicator">The requested indicator.</param>
    /// <returns>The resource server, or <see langword="null"/> when the indicator is unknown or malformed.</returns>
    public ResourceServer? FindByIndicator(string indicator) => _catalog.FindByIndicator(indicator);

    /// <summary>Finds a resource server by its audience.</summary>
    /// <param name="audience">The audience.</param>
    /// <returns>The resource server, or <see langword="null"/> when the audience is unknown.</returns>
    public ResourceServer? FindByAudience(string audience) => _catalog.FindByAudience(audience);

    /// <summary>Finds the audiences of the resource servers that own a scope.</summary>
    /// <param name="scope">The scope name.</param>
    /// <returns>The audiences; empty when no resource server owns the scope.</returns>
    public string[] AudiencesOwning(string scope) => [.. _catalog.FindByScope(scope).Select(_ => _.Audience)];

    /// <summary>Lists every known scope name with its description, in catalog order.</summary>
    /// <returns>The scope names and descriptions.</returns>
    public async Task<(string Name, string? Description)[]> Scopes()
    {
        var scopes = new List<(string, string?)>();
        await foreach (var scope in _scopes.ListAsync(null, null, CancellationToken.None))
        {
            scopes.Add((await _scopes.GetNameAsync(scope, CancellationToken.None) ?? string.Empty, await _scopes.GetDescriptionAsync(scope, CancellationToken.None)));
        }

        return [.. scopes];
    }

    /// <summary>Finds whether a scope name is known to the scope store.</summary>
    /// <param name="name">The exact scope name.</param>
    /// <returns><see langword="true"/> when the scope is known.</returns>
    public async Task<bool> HasScope(string name) => await _scopes.FindByNameAsync(name, CancellationToken.None) is not null;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _provider.DisposeAsync();
    }
}
