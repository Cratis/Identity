// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Tenants;

/// <summary>Registers the configured and callback-backed tenant directory.</summary>
internal static class TenantServiceCollectionExtensions
{
    /// <summary>Adds the tenant directory.</summary>
    /// <param name="services">The services.</param>
    internal static void AddTenants(this IServiceCollection services)
    {
        services.AddSingleton<ConfiguredTenantDirectory>();
        services.AddSingleton<ProductTenantsCallback>();
        services.AddSingleton<ITenantDirectory, TenantDirectory>();
        services.AddHttpClient(TenantsCallbackHttpClient.Name, client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(TenantsCallbackHttpClient.CreateHandler)
            .RemoveAllLoggers();
    }
}
