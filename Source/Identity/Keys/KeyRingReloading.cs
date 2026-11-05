// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Keys;

/// <summary>
/// Reads the secret store again periodically, and at the moment a key becomes eligible or expires, so a published
/// or retired key takes effect without a restart.
/// </summary>
/// <remarks>
/// Creating the service creates <see cref="SecretStoreKeys"/>, which validates the store, so an invalid store stops the host at startup.
/// </remarks>
/// <param name="keys">The <see cref="SecretStoreKeys"/>.</param>
/// <param name="options">The <see cref="IdentityOptions"/>, for the reload interval.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
public class KeyRingReloading(SecretStoreKeys keys, IOptions<IdentityOptions> options, TimeProvider timeProvider) : BackgroundService
{
    static readonly TimeSpan _shortest = TimeSpan.FromSeconds(1);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(NextDelay(), timeProvider, stoppingToken);
                keys.Reload();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopping.
        }
    }

    TimeSpan NextDelay()
    {
        var delay = options.Value.SecretStore.ReloadInterval;
        if (keys.NextBoundary is { } boundary && boundary - timeProvider.GetUtcNow() is var untilBoundary && untilBoundary < delay)
        {
            delay = untilBoundary;
        }

        return delay < _shortest ? _shortest : delay;
    }
}
