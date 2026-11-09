// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Identity.Protocol.for_ReadModelCatchUp;

public class when_the_request_is_cancelled_while_waiting_for_a_projection : given.projection_progress
{
    CancellationTokenSource _request;
    Exception _cancelled;

    void Establish()
    {
        _progress.Position(Arg.Any<Type>()).Returns(Task.FromResult(new ObserverPosition(ObserverRunningState.Active, false, false, new(4))));
        _request = new();
        _request.Cancel();
    }

    async Task Because() => _cancelled = await Catch.Exception(() => _catchUp.BeforeRetry(_request.Token));

    void Destroy() => _request.Dispose();

    [Fact] void should_observe_the_caller_cancellation() => (_cancelled is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_report_the_projections_as_not_caught_up() => (_cancelled is ReadModelsNotCaughtUp).ShouldBeFalse();
}
