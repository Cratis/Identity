// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Identity.Protocol;

/// <summary>Reads projection-specific progress without using the whole-log tail.</summary>
/// <param name="store">The scoped realm event store.</param>
internal sealed class ObserverProgress(IEventStore store) : IObserverProgress
{
    /// <inheritdoc/>
    public Task<EventSequenceNumber> Tail(Type readModel) => store.EventLog.GetTailSequenceNumber(
        filterEventTypes: ProjectionInputs.For(readModel).Select(store.EventTypes.GetEventTypeFor).ToArray());

    /// <inheritdoc/>
    public async Task<ObserverPosition> Position(Type readModel)
    {
        var state = await store.Projections.GetStateForModel(readModel);
        var failed = await store.Projections.GetFailedPartitionsForModel(readModel);
        return new(state.RunningState, failed.Any(), state.RunningState == ObserverRunningState.Replaying, state.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    public Task<AppendResultWaitForCompletionResult> Complete(AppendResult result, TimeSpan timeout) => result.WaitForCompletion(timeout);
}
