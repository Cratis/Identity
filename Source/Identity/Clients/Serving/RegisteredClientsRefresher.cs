// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Identity.Clients.Serving;

/// <summary>
/// Loads the registered clients when Cratis Identity starts and keeps them current, so a registration, a rotation or a
/// retirement is served within one interval.
/// </summary>
/// <remarks>
/// It reads on a schedule instead of observing the registration events, because observers are limited to building read
/// models and publishing (see the observer architecture specification); serving clients is a read of what they built.
/// </remarks>
/// <param name="clients">The served clients.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class RegisteredClientsRefresher(IRegisteredClients clients, ILogger<RegisteredClientsRefresher> logger) : BackgroundService
{
    /// <summary>How often the registrations are read again.</summary>
    internal static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await clients.Refresh(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                RefreshFailed(logger, exception);
            }
        }
        while (await WaitForNext(timer, stoppingToken));
    }

    static async Task<bool> WaitForNext(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    [LoggerMessage(LogLevel.Warning, "The registered clients could not be read; the ones already served stay in place")]
    static partial void RefreshFailed(ILogger logger, Exception exception);
}
