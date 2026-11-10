// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Operational;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts;

/// <summary>Registers native account admission and lookup-key startup protection.</summary>
internal static class AccountServiceCollectionExtensions
{
    /// <summary>Adds the native account services.</summary>
    /// <param name="services">The services.</param>
    /// <returns>The services for continuation.</returns>
    internal static IServiceCollection AddAccountServices(this IServiceCollection services)
    {
        services.AddSingleton<IUpstreamLookupKeys, ConfiguredUpstreamLookupKeys>();
        services.AddSingleton<UpstreamKeyDigester>();
        services.AddSingleton<VerifiedKeyGate>();
        services.AddScoped<UpstreamLookups>();
        services.AddScoped<IUpstreamKeyResolver, UpstreamKeyResolver>();
        services.AddScoped<AccountReader>();
        services.AddScoped<IAccountReader>(provider => provider.GetRequiredService<AccountReader>());
        services.AddScoped<EventSourcedAccountAdapter>();
        services.AddScoped<IAccountSignIn>(provider => provider.GetRequiredService<EventSourcedAccountAdapter>());
        services.AddScoped<VerifiedUpstreamSignIn>();
        services.AddScoped<IVerifiedUpstreamSignIn>(provider => provider.GetRequiredService<VerifiedUpstreamSignIn>());
        services.AddScoped<LinkCeremony>();
        services.AddScoped<ILinkCeremony>(provider => provider.GetRequiredService<LinkCeremony>());
        services.AddScoped<AccountLinking>();
        services.AddLookupRevisionGuard();
        return services;
    }
}
