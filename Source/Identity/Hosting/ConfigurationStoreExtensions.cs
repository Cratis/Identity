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
    /// <returns>The builder for chaining configuration.</returns>
    internal static OpenIddictCoreBuilder UseConfigurationStores(this OpenIddictCoreBuilder builder)
    {
        builder.Services.TryAddSingleton<IProtocolCatalog, ConfigurationProtocolCatalog>();
        return builder
            .SetDefaultApplicationEntity<ClientApplication>()
            .SetDefaultScopeEntity<ResourceScope>()
            .ReplaceApplicationStore<ClientApplication, ConfigurationApplicationStore>()
            .ReplaceScopeStore<ResourceScope, ConfigurationScopeStore>();
    }
}
