// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Storage;

namespace Cratis.Identity.Clients;

/// <summary>Replaces the legacy startup writer only when Chronicle-backed clients are opted in.</summary>
/// <param name="scopes">The service scope factory.</param>
/// <param name="logger">The startup logger.</param>
public class EventSourcedClientRegistrations(IServiceScopeFactory scopes, ILogger<EventSourcedClientRegistrations> logger) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var services = scope.ServiceProvider;
        await services.GetRequiredService<StorageIndexes>().Ensure(cancellationToken);
        var count = await services.GetRequiredService<ClientRegistrationReconciler>()
            .Reconcile(services.GetRequiredService<ClientDescriptors>().All(), cancellationToken);
        logger.EventSourcedClientsReconciled(count);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
