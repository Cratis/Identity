// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Cratis.Identity.Hosting;

/// <summary>Opt-in wiring only; no production host invokes it by default.</summary>
public static class EventSourcedAccountServiceCollectionExtensions
{
    /// <summary>Adds the opt-in account path without replacing IAccounts or configuring a Chronicle host.</summary>
    /// <param name="services">The services.</param>
    /// <returns>The collection with the opt-in registrations.</returns>
    public static IServiceCollection AddEventSourcedAccounts(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IUpstreamLookupKeys, ConfiguredUpstreamLookupKeys>();
        services.AddSingleton<UpstreamKeyDigester>();
        services.AddScoped<AccountStore>();
        services.AddScoped<UpstreamLookups>();
        services.AddScoped<EventSourcedAccounts>();
        services.AddScoped<UpstreamKeyLinking>();
        services.TryAddSingleton(provider => provider.GetRequiredService<IMongoDatabase>().GetCollection<LookupRevision>("account_lookup_revisions"));
        services.AddHostedService<LookupRevisionGuard>();
        return services;
    }
}
