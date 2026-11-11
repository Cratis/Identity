// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Protocol.Stores;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Identity.Hosting;

/// <summary>Registers the configuration-backed application and scope stores.</summary>
internal static class ConfigurationStoreExtensions
{
    /// <summary>Uses read-only application and scope read models.</summary>
    /// <param name="builder">The core builder.</param>
    /// <param name="metadataEnabled">Whether bounded CIMD caching replaces OpenIddict entity caching.</param>
    /// <returns>The builder for chaining configuration.</returns>
    internal static OpenIddictCoreBuilder UseConfigurationStores(this OpenIddictCoreBuilder builder, bool metadataEnabled = false)
    {
        builder.Services.TryAddSingleton<IProtocolCatalog, ConfigurationProtocolCatalog>();

        // OpenIddict caches both misses and positive applications indefinitely. CIMD freshness and repair
        // must be decided by the bounded metadata cache on every manager lookup, not by that entity cache.
        if (metadataEnabled) builder.DisableEntityCaching();
        return builder
            .SetDefaultApplicationEntity<ClientApplication>()
            .SetDefaultScopeEntity<ResourceScope>()
            .ReplaceApplicationStore<ClientApplication, ConfigurationApplicationStore>()
            .ReplaceScopeStore<ResourceScope, ConfigurationScopeStore>();
    }
}
