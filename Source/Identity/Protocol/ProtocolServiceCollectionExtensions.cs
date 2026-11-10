// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Identity.Protocol;

/// <summary>Registers the command and read-model catch-up boundary shared by protocol endpoints.</summary>
internal static class ProtocolServiceCollectionExtensions
{
    /// <summary>Adds the protocol boundary services.</summary>
    /// <param name="services">The services.</param>
    internal static void AddProtocolServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IProtocolCommands, ProtocolCommands>();
        services.AddScoped<IObserverProgress, ObserverProgress>();
        services.AddScoped<ReadModelCatchUp>();
    }
}
