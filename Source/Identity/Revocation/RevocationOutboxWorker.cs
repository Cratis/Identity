// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Bounded polling worker; request-time denial never depends on its progress.</summary>
/// <param name="scopes">The per-pass scope factory.</param>
/// <param name="logger">The diagnostic logger; no payloads or operational identifiers are logged.</param>
internal sealed class RevocationOutboxWorker(IServiceScopeFactory scopes, ILogger<RevocationOutboxWorker> logger) : BackgroundService
{
    static readonly Action<ILogger, Exception?> _held = LoggerMessage.Define(LogLevel.Warning, new EventId(1, "RevocationOutboxHeld"), "Revocation outbox delivery held; durable intents will be retried.");

    static readonly Action<ILogger, Exception?> _conflict = LoggerMessage.Define(LogLevel.Critical, new EventId(2, "RevocationOutboxConflict"), "Revocation outbox content conflict; delivery is held for operator investigation.");

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delaySeconds = 5;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DispatchRevocationOutbox>().Dispatch(stoppingToken);
                delaySeconds = 5;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Do not put store exceptions (which can contain submitted values) into diagnostics.
                if (exception is OutboxContentConflict or FenceLogConflict)
                {
                    _conflict(logger, null);
                }
                else
                {
                    _held(logger, null);
                }

                delaySeconds = Math.Min(delaySeconds * 2, 60);
            }

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }
}
