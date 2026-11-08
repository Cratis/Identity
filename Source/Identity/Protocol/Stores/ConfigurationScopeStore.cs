// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Protocol.Stores;

/// <summary>Serves scope reads without persisting configuration to MongoDB.</summary>
/// <param name="catalog">The scope read model source.</param>
internal sealed class ConfigurationScopeStore(IProtocolCatalog catalog) : IOpenIddictScopeStore<ResourceScope>
{
    /// <inheritdoc/>
    public ValueTask<long> CountAsync(CancellationToken cancellationToken) => StoreQueries.Read((long)catalog.Scopes.Count, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<ResourceScope>, IQueryable<TResult>> query, CancellationToken cancellationToken) => StoreQueries.Read(query(catalog.Scopes.AsQueryable()).LongCount(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ResourceScope?> FindByIdAsync(string identifier, CancellationToken cancellationToken) => FindByNameAsync(identifier, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ResourceScope?> FindByNameAsync(string name, CancellationToken cancellationToken) => StoreQueries.Read(catalog.Scopes.FirstOrDefault(_ => _.Name == name), cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<ResourceScope> FindByNamesAsync(ImmutableArray<string> names, CancellationToken cancellationToken) => StoreQueries.Enumerate(catalog.Scopes.Where(_ => names.Contains(_.Name, StringComparer.Ordinal)), cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<ResourceScope> FindByResourceAsync(string resource, CancellationToken cancellationToken) => StoreQueries.Enumerate(catalog.Scopes.Where(_ => _.Descriptor.Resources.Contains(resource)), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<ResourceScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken) => StoreQueries.Read(query(catalog.Scopes.AsQueryable(), state).FirstOrDefault(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetDescriptionAsync(ResourceScope scope, CancellationToken cancellationToken) => StoreQueries.Read(scope.Descriptor.Description, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDescriptionsAsync(ResourceScope scope, CancellationToken cancellationToken) => StoreQueries.Read(scope.Descriptor.Descriptions.ToImmutableDictionary(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetDisplayNameAsync(ResourceScope scope, CancellationToken cancellationToken) => StoreQueries.Read(scope.Descriptor.DisplayName, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(ResourceScope scope, CancellationToken cancellationToken) => StoreQueries.Read(scope.Descriptor.DisplayNames.ToImmutableDictionary(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetIdAsync(ResourceScope scope, CancellationToken cancellationToken) => StoreQueries.Read<string?>(scope.Name, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetNameAsync(ResourceScope scope, CancellationToken cancellationToken) => StoreQueries.Read<string?>(scope.Name, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(ResourceScope scope, CancellationToken cancellationToken) => StoreQueries.Read(scope.Descriptor.Properties.ToImmutableDictionary(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetResourcesAsync(ResourceScope scope, CancellationToken cancellationToken) => StoreQueries.Read(scope.Descriptor.Resources.ToImmutableArray(), cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<ResourceScope> ListAsync(int? count, int? offset, CancellationToken cancellationToken) => StoreQueries.Enumerate(StoreQueries.Page(catalog.Scopes, count, offset), cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<ResourceScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken) => StoreQueries.Enumerate(query(catalog.Scopes.AsQueryable(), state), cancellationToken);

    /// <inheritdoc/>
    public ValueTask CreateAsync(ResourceScope scope, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask DeleteAsync(ResourceScope scope, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask UpdateAsync(ResourceScope scope, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask<ResourceScope> InstantiateAsync(CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetDescriptionAsync(ResourceScope scope, string? description, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetDescriptionsAsync(ResourceScope scope, ImmutableDictionary<CultureInfo, string> descriptions, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetDisplayNameAsync(ResourceScope scope, string? name, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetDisplayNamesAsync(ResourceScope scope, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetNameAsync(ResourceScope scope, string? name, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetPropertiesAsync(ResourceScope scope, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetResourcesAsync(ResourceScope scope, ImmutableArray<string> resources, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();
}
