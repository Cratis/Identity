// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Identity.Operational;

/// <summary>Registers the startup guard that protects accounts left in the removed MongoDB account store.</summary>
public static class LegacyAccountStoreServiceCollectionExtensions
{
    /// <summary>Adds the guard. Register it before every other hosted service that touches MongoDB, because hosted services start in registration order.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The services with the guard registered.</returns>
    public static IServiceCollection AddLegacyAccountStoreGuard(this IServiceCollection services)
    {
        services.TryAddSingleton<ILegacyAccountStoreProbe, LegacyAccountStoreProbe>();
        services.AddHostedService<LegacyAccountStoreGuard>();
        return services;
    }
}
