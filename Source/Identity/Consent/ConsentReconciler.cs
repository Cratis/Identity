// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Identity.ConsentFlow;

/// <summary>Resumes bounded, fairly paged operational work in fresh scopes; never issues browser codes.</summary>
/// <param name="scopes">The scope factory.</param>
/// <param name="options">The reconciliation settings.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
internal sealed class ConsentReconciler(
    IServiceScopeFactory scopes,
    IOptions<ConsentActivationOptions> options,
    TimeProvider timeProvider,
    ILogger<ConsentReconciler> logger) : BackgroundService
{
    ConsentRequestId? _after;

    /// <summary>Runs one bounded pass. A failed request does not prevent later requests from resuming.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pass task.</returns>
    public async Task Reconcile(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IConsentRequestStore>();
        var activation = scope.ServiceProvider.GetRequiredService<ConsentActivation>();
        var requests = await store.Unfinished(options.Value.BatchSize, _after, cancellationToken);
        if (requests.Count == 0)
        {
            _after = null;
            return;
        }

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await activation.Resume(request.Id, cancellationToken);
                if (result?.State is ConsentRequestState.Decided or ConsentRequestState.EvidenceRecorded or ConsentRequestState.EvidenceWithheld)
                {
                    logger.RequestUnresolved(request.Id.Value, result.State);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                logger.RequestRetry(request.Id.Value);
            }

            _after = request.Id;
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Reconcile(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                logger.StoreUnavailable();
            }

            await Task.Delay(options.Value.ReconcileInterval, timeProvider, stoppingToken);
        }
    }
}
