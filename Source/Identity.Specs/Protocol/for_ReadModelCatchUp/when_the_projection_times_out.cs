// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Identity.Protocol.for_ReadModelCatchUp;

public class when_the_projection_times_out : given.projection_progress
{
    void Establish() => _progress.Position(Arg.Any<Type>()).Returns(Task.FromResult(new ObserverPosition(ObserverRunningState.Active, false, false, new(4))));

    Task Because() => Wait();

    [Fact] void should_refuse_the_retry() => _error.ShouldBeOfExactType<ReadModelsNotCaughtUp>();
}
