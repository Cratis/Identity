// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Identity.Protocol.for_ReadModelCatchUp;

public class when_a_projection_cannot_make_progress : given.projection_progress
{
    void Establish() => _progress.Position(Arg.Any<Type>()).Returns(Task.FromResult(new ObserverPosition(ObserverRunningState.Suspended, false, false, new(5))));

    Task Because() => Wait();

    [Fact] void should_refuse_the_retry() => _error.ShouldBeOfExactType<ReadModelsNotCaughtUp>();
    [Fact] void should_report_the_closed_reason() => _error.Message.ShouldEqual("read_models_not_caught_up");
}
