// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Integration.given;

/// <summary>Retains native filtered targets and append waits while allowing a test-local progress hold.</summary>
/// <param name="native">The real Chronicle progress reader.</param>
/// <param name="gate">The owning test's optional hold.</param>
public sealed class GatedObserverProgress(ObserverProgress native, ProjectionProgressGate gate) : IObserverProgress
{
    /// <inheritdoc/>
    public Task<EventSequenceNumber> Tail(Type readModel) => native.Tail(readModel);

    /// <inheritdoc/>
    public async Task<ObserverPosition> Position(Type readModel) => gate.Position(readModel) ?? await native.Position(readModel);

    /// <inheritdoc/>
    public Task<AppendResultWaitForCompletionResult> Complete(AppendResult result, TimeSpan timeout) => native.Complete(result, timeout);
}
