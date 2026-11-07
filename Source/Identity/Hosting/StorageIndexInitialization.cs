// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Storage;

namespace Cratis.Identity.Hosting;

/// <summary>Initializes operational store indexes after the legacy account guard has run.</summary>
/// <param name="scopes">The service scope factory.</param>
internal sealed class StorageIndexInitialization(IServiceScopeFactory scopes) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<StorageIndexes>().Ensure(cancellationToken);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
