// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation.Operational;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Identity.Revocation;

/// <summary>Registers independent recovery, fencing and revocation dispatch.</summary>
internal static class RevocationServiceCollectionExtensions
{
    /// <summary>Adds the revocation services.</summary>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The Identity configuration.</param>
    internal static void AddRevocation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration.GetSection("Identity:FenceLog").Get<FenceLogOptions>() ?? new FenceLogOptions());
        services.AddSingleton<IndependentFenceLogConnection>();
        services.AddSingleton(IndependentFenceLogFactory.Create);
        services.AddSingleton<IIndependentFenceLog>(sp => sp.GetRequiredService<MongoIndependentFenceLog>());
        services.AddSingleton<IRecoveryLog>(sp => sp.GetRequiredService<MongoIndependentFenceLog>());
        services.AddSingleton<IReservationLog>(sp => sp.GetRequiredService<MongoIndependentFenceLog>());
        services.AddSingleton<MongoFenceStore>();
        services.TryAddSingleton<IFenceStore>(sp => sp.GetRequiredService<MongoFenceStore>());
        services.AddSingleton<IReservationAuthority>(sp => sp.GetRequiredService<MongoFenceStore>());
        services.AddSingleton<IRecoveryStore, MongoFenceRecovery>();
        services.AddSingleton<RecoveryGate>();
        services.AddSingleton<RecoverySession>();
        services.AddScoped<ReconcileReservation>();
        services.AddScoped<ReconcileRecovery>();
        services.AddScoped<IRecoveryFactWriter, ChronicleRecoveryFactWriter>();
        services.TryAddSingleton<IFenceLog, IndependentFenceLogAppender>();
        services.AddHealthChecks().AddCheck<RecoveryReadiness>("identity-restriction-recovery");
        services.AddScoped<ConfirmFenceLog>();
        services.TryAddScoped<IRevocationFactWriter, ChronicleRevocationFactWriter>();
        services.AddScoped<DispatchRevocationOutbox>();
        services.AddHostedService<RevocationOutboxWorker>();
    }
}
