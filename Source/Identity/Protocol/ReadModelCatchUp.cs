// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Protocol;

/// <summary>Bounds acknowledgement and decision retries by actual projection progress.</summary>
/// <param name="progress">The Chronicle progress adapter.</param>
/// <param name="options">The wait budget.</param>
/// <param name="time">The clock.</param>
public sealed class ReadModelCatchUp(IObserverProgress progress, IOptions<IdentityOptions> options, TimeProvider time)
{
    /// <summary>Waits for the observers named by successful appends from this command.</summary>
    /// <param name="appends">The captured appends, not a log-tail fence.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The completion task.</returns>
    /// <exception cref="ReadModelsNotCaughtUp">An observer cannot complete within the budget.</exception>
    public async Task AfterCommand(IEnumerable<AppendedEventWithResult> appends, CancellationToken cancellationToken)
    {
        using var budget = Budget(cancellationToken);
        try
        {
            foreach (var append in appends.Where(append => append.Result.IsSuccess))
            {
                var completed = await progress.Complete(append.Result, options.Value.Accounts.ReadModelCatchUpTimeout).WaitAsync(budget.Token);
                if (!completed.IsSuccess)
                {
                    throw new ReadModelsNotCaughtUp();
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ReadModelsNotCaughtUp();
        }
    }

    /// <summary>Captures each projection's filtered target once and waits for that exact target.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The completion task.</returns>
    /// <exception cref="ReadModelsNotCaughtUp">A projection is failed, stopped, disconnected, replaying or timed out.</exception>
    public async Task BeforeRetry(CancellationToken cancellationToken)
    {
        using var budget = Budget(cancellationToken);
        try
        {
            var targets = new Dictionary<Type, EventSequenceNumber>();
            foreach (var model in ProjectionInputs.SignInModels)
            {
                targets.Add(model, await progress.Tail(model).WaitAsync(budget.Token));
            }

            while (true)
            {
                var ready = true;
                foreach (var (model, target) in targets)
                {
                    var position = await progress.Position(model).WaitAsync(budget.Token);
                    if (position.HasFailedPartitions || position.IsReplaying || position.RunningState != ObserverRunningState.Active)
                    {
                        throw new ReadModelsNotCaughtUp();
                    }

                    ready &= target == EventSequenceNumber.Unavailable ||
                        (position.LastHandled != EventSequenceNumber.Unavailable && position.LastHandled.Value >= target.Value);
                }

                if (ready)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(25), time, budget.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ReadModelsNotCaughtUp();
        }
    }

    CancellationTokenSource Budget(CancellationToken cancellationToken)
    {
        var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(options.Value.Accounts.ReadModelCatchUpTimeout);
        return budget;
    }
}
