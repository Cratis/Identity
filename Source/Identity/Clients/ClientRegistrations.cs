// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Storage;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Clients;

/// <summary>
/// Prepares storage and reconciles the client store with <see cref="ClientDescriptors"/> at startup: declared
/// clients are created or updated, and any other client is deleted, so a client removed from configuration
/// stops working at the next start.
/// </summary>
/// <param name="scopes">The <see cref="IServiceScopeFactory"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
internal class ClientRegistrations(IServiceScopeFactory scopes, ILogger<ClientRegistrations> logger) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<StorageIndexes>().Ensure(cancellationToken);

        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var declared = scope.ServiceProvider.GetRequiredService<ClientDescriptors>().All().ToArray();

        foreach (var descriptor in declared)
        {
            var existing = await applications.FindByClientIdAsync(descriptor.ClientId!, cancellationToken);
            if (existing is null)
            {
                await applications.CreateAsync(descriptor, cancellationToken);
            }
            else
            {
                await applications.UpdateAsync(existing, descriptor, cancellationToken);
            }
        }

        var declaredIds = declared.Select(_ => _.ClientId!).ToHashSet(StringComparer.Ordinal);
        var stale = new List<object>();
        await foreach (var application in applications.ListAsync(count: null, offset: null, cancellationToken))
        {
            if (!declaredIds.Contains(await applications.GetClientIdAsync(application, cancellationToken) ?? string.Empty))
            {
                stale.Add(application);
            }
        }

        foreach (var application in stale)
        {
            logger.RemovingUndeclaredClient(await applications.GetClientIdAsync(application, cancellationToken));
            await applications.DeleteAsync(application, cancellationToken);
        }

        logger.ClientsReconciled(declared.Length);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
