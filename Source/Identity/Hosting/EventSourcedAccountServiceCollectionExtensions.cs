// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Operational;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddSingleton<IProtocolCommands, ProtocolCommands>();
        services.AddScoped<IObserverProgress, ObserverProgress>();
        services.AddScoped<ReadModelCatchUp>();
        services.AddSingleton<IUpstreamLookupKeys, ConfiguredUpstreamLookupKeys>();
        services.AddSingleton<UpstreamKeyDigester>();
        services.AddSingleton<VerifiedKeyGate>();
        services.AddScoped<AccountStore>();
        services.AddScoped<UpstreamLookups>();
        services.AddScoped<AccountReader>();
        services.AddScoped<EventSourcedAccountAdapter>();
        services.AddScoped<VerifiedUpstreamSignIn>();
        services.AddScoped<IVerifiedUpstreamSignIn>(provider => provider.GetRequiredService<VerifiedUpstreamSignIn>());
        services.AddScoped<UpstreamKeyLinking>();
        services.AddLookupRevisionGuard();
        return services;
    }
}
