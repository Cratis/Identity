// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.Revocation;

/// <summary>Bounded polling worker; request-time denial never depends on its progress.</summary>
/// <param name="scopes">The per-pass scope factory.</param>
/// <param name="logger">The diagnostic logger; no payloads or operational identifiers are logged.</param>
/// <param name="gate">The host recovery barrier.</param>
internal sealed class RevocationOutboxWorker(IServiceScopeFactory scopes, ILogger<RevocationOutboxWorker> logger, RecoveryGate gate) : BackgroundService
{
    static readonly Action<ILogger, Exception?> _held = LoggerMessage.Define(LogLevel.Warning, new EventId(1, "RevocationOutboxHeld"), "Revocation outbox delivery held; durable intents will be retried.");

    static readonly Action<ILogger, Exception?> _conflict = LoggerMessage.Define(LogLevel.Critical, new EventId(2, "RevocationOutboxConflict"), "Revocation outbox content conflict; delivery is held for operator investigation.");

    /// <summary>Attempts delivery even when a recovery gap keeps admission closed.</summary>
    /// <param name="recovery">The recovery pass.</param>
    /// <param name="dispatch">The delivery pass.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The recovery and delivery attempt.</returns>
    internal static async Task RecoverAndDeliver(ReconcileRecovery recovery, DispatchRevocationOutbox dispatch, CancellationToken cancellationToken)
    {
        try
        {
            await recovery.Run(cancellationToken);
        }
        finally
        {
            await recovery.PublishEpochs(cancellationToken);
            await dispatch.Dispatch(cancellationToken);
        }

        await recovery.ConfirmPublication(cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var meter = new Meter("Cratis.Identity.Recovery");
        meter.CreateObservableGauge("identity_recovery_unready_seconds", () => gate.UnreadySeconds, "s");
        var delaySeconds = 5;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var session = scope.ServiceProvider.GetRequiredService<RecoverySession>();
                if (!session.Verified)
                {
                    await scope.ServiceProvider.GetRequiredService<IndependentFenceLogConnection>().Verify(stoppingToken);
                    session.Verified = true;
                }

                var recovery = scope.ServiceProvider.GetRequiredService<ReconcileRecovery>();
                await RecoverAndDeliver(recovery, scope.ServiceProvider.GetRequiredService<DispatchRevocationOutbox>(), stoppingToken);
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
