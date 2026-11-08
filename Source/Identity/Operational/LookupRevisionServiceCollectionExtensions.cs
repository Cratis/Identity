// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Cratis.Identity.Operational;

/// <summary>Registers the operational lookup revision guard without exposing MongoDB to account slices.</summary>
internal static class LookupRevisionServiceCollectionExtensions
{
    /// <summary>Adds the realm's serialized revision startup guard.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The services with the operational revision guard registered.</returns>
    public static IServiceCollection AddLookupRevisionGuard(this IServiceCollection services)
    {
        services.TryAddSingleton(provider => provider.GetRequiredService<IMongoDatabase>().GetCollection<LookupRevision>("account_lookup_revisions"));
        services.AddHostedService<LookupRevisionGuard>();
        return services;
    }
}
