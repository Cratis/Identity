// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Clients;
using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Clients.Secrets;
using Cratis.Identity.Storage;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Opt-in registration for the Chronicle client configuration writer.</summary>
internal static class EventSourcedClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers the opt-in client writer. Requires the host to register a Chronicle client before
    /// AddCratisIdentity; a missing IEventStore fails ValidateOnBuild.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    internal static IServiceCollection AddEventSourcedClients(this IServiceCollection services)
    {
        services.TryAddScoped<IReadModels>(provider => provider.GetRequiredService<IEventStore>().ReadModels);
        services.TryAddSingleton<IClientSecretDigestKeys, ConfiguredClientSecretDigestKeys>();
        services.TryAddSingleton<ClientSecretDigester>();
        services.TryAddSingleton<ClientMaterializations>();
        services.AddScoped<ClientRegistrationStore>();
        services.AddScoped<ClientRegistrationLookups>();
        services.AddScoped<ClientRegistrationReconciler>();
        services.AddHostedService<EventSourcedClientRegistrations>();
        return services;
    }
}
