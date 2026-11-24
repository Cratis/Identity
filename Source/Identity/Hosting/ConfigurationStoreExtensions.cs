// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Serving;
using Cratis.Identity.Protocol.Stores;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Identity.Hosting;

/// <summary>Registers the configuration-backed application and scope stores.</summary>
internal static class ConfigurationStoreExtensions
{
    /// <summary>Uses read-only application and scope read models.</summary>
    /// <param name="builder">The core builder.</param>
    /// <param name="metadataEnabled">Whether metadata documents are enabled. Entity caching is off either way, so this no longer changes what is registered.</param>
    /// <returns>The builder for chaining configuration.</returns>
    internal static OpenIddictCoreBuilder UseConfigurationStores(this OpenIddictCoreBuilder builder, bool metadataEnabled = false)
    {
        _ = metadataEnabled;
        builder.Services.TryAddSingleton<ConfigurationProtocolCatalog>();
        builder.Services.TryAddSingleton<IRegisteredClients, RegisteredClients>();
        builder.Services.TryAddSingleton<IProtocolCatalog>(services => new ServedProtocolCatalog(
            services.GetRequiredService<ConfigurationProtocolCatalog>(),
            services.GetRequiredService<IRegisteredClients>()));
        builder.Services.AddHostedService<RegisteredClientsRefresher>();

        // OpenIddict caches both misses and positive applications indefinitely. CIMD freshness and repair
        // must be decided by the bounded metadata cache on every manager lookup, not by that entity cache.
        // Clients registered while Identity runs appear, change their redirect addresses and rotate their secrets, so a cached
        // hit or miss would keep serving the old answer.
        builder.DisableEntityCaching();
        return builder
            .SetDefaultApplicationEntity<ClientApplication>()
            .SetDefaultScopeEntity<ResourceScope>()
            .ReplaceApplicationManager(typeof(ClientApplicationManager<>))
            .ReplaceApplicationStore<ClientApplication, ConfigurationApplicationStore>()
            .ReplaceScopeStore<ResourceScope, ConfigurationScopeStore>();
    }
}
