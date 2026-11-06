// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Identity.Protocol;

/// <summary>The Chronicle observer-progress boundary used by protocol catch-up.</summary>
public interface IObserverProgress
{
    /// <summary>Captures a target filtered to the model's actual projection input event types.</summary>
    /// <param name="readModel">The model.</param>
    /// <returns>The filtered tail.</returns>
    Task<EventSequenceNumber> Tail(Type readModel);

    /// <summary>Reads the current projection position and failure state.</summary>
    /// <param name="readModel">The model.</param>
    /// <returns>The observer position.</returns>
    Task<ObserverPosition> Position(Type readModel);

    /// <summary>Waits for the observers interested in one actual append result.</summary>
    /// <param name="result">The append result.</param>
    /// <param name="timeout">The remaining wait budget.</param>
    /// <returns>The completion status.</returns>
    Task<AppendResultWaitForCompletionResult> Complete(AppendResult result, TimeSpan timeout);
}
