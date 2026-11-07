// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Protocol.Stores;

/// <summary>Serves application reads without persisting configuration to MongoDB.</summary>
/// <param name="catalog">The application read model source.</param>
internal sealed class ConfigurationApplicationStore(IProtocolCatalog catalog) : IOpenIddictApplicationStore<ClientApplication>
{
    /// <inheritdoc/>
    public ValueTask<long> CountAsync(CancellationToken cancellationToken) => StoreQueries.Read((long)catalog.Applications.Count, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<ClientApplication>, IQueryable<TResult>> query, CancellationToken cancellationToken) => StoreQueries.Read(query(catalog.Applications.AsQueryable()).LongCount(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ClientApplication?> FindByIdAsync(string identifier, CancellationToken cancellationToken) => StoreQueries.Read(catalog.Applications.FirstOrDefault(_ => _.Id == identifier), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ClientApplication?> FindByClientIdAsync(string identifier, CancellationToken cancellationToken) => StoreQueries.Read(catalog.Applications.FirstOrDefault(_ => _.ClientId == identifier), cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<ClientApplication> FindByPostLogoutRedirectUriAsync(string uri, CancellationToken cancellationToken) => StoreQueries.Enumerate(catalog.Applications.Where(_ => _.Descriptor.PostLogoutRedirectUris.Any(value => value.OriginalString == uri)), cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<ClientApplication> FindByRedirectUriAsync(string uri, CancellationToken cancellationToken) => StoreQueries.Enumerate(catalog.Applications.Where(_ => _.Descriptor.RedirectUris.Any(value => value.OriginalString == uri)), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<ClientApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken) => StoreQueries.Read(query(catalog.Applications.AsQueryable(), state).FirstOrDefault(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetApplicationTypeAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.ApplicationType, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetClientIdAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read<string?>(application.ClientId, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetClientSecretAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.ClientSecret, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetClientTypeAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.ClientType, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetConsentTypeAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.ConsentType, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetDisplayNameAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.DisplayName, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.DisplayNames.ToImmutableDictionary(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<string?> GetIdAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read<string?>(application.Id, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<JsonWebKeySet?> GetJsonWebKeySetAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.JsonWebKeySet, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetPermissionsAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.Permissions.ToImmutableArray(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.PostLogoutRedirectUris.Select(_ => _.OriginalString).ToImmutableArray(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.Properties.ToImmutableDictionary(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetRedirectUrisAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.RedirectUris.Select(_ => _.OriginalString).ToImmutableArray(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetRequirementsAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.Requirements.ToImmutableArray(), cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<string, string>> GetSettingsAsync(ClientApplication application, CancellationToken cancellationToken) => StoreQueries.Read(application.Descriptor.Settings.ToImmutableDictionary(), cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<ClientApplication> ListAsync(int? count, int? offset, CancellationToken cancellationToken) => StoreQueries.Enumerate(StoreQueries.Page(catalog.Applications, count, offset), cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<ClientApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken) => StoreQueries.Enumerate(query(catalog.Applications.AsQueryable(), state), cancellationToken);

    /// <inheritdoc/>
    public ValueTask CreateAsync(ClientApplication application, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask DeleteAsync(ClientApplication application, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask UpdateAsync(ClientApplication application, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask<ClientApplication> InstantiateAsync(CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetApplicationTypeAsync(ClientApplication application, string? type, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetClientIdAsync(ClientApplication application, string? identifier, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetClientSecretAsync(ClientApplication application, string? secret, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetClientTypeAsync(ClientApplication application, string? type, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetConsentTypeAsync(ClientApplication application, string? type, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetDisplayNameAsync(ClientApplication application, string? name, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetDisplayNamesAsync(ClientApplication application, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetJsonWebKeySetAsync(ClientApplication application, JsonWebKeySet? set, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetPermissionsAsync(ClientApplication application, ImmutableArray<string> permissions, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetPostLogoutRedirectUrisAsync(ClientApplication application, ImmutableArray<string> uris, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetPropertiesAsync(ClientApplication application, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetRedirectUrisAsync(ClientApplication application, ImmutableArray<string> uris, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetRequirementsAsync(ClientApplication application, ImmutableArray<string> requirements, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();

    /// <inheritdoc/>
    public ValueTask SetSettingsAsync(ClientApplication application, ImmutableDictionary<string, string> settings, CancellationToken cancellationToken) => throw StoreQueries.ReadOnly();
}
