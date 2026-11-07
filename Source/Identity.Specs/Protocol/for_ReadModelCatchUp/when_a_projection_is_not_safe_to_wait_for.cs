// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Identity.Protocol.for_ReadModelCatchUp;

public class when_a_projection_is_not_safe_to_wait_for : given.projection_progress
{
    [Theory]
    [InlineData(ObserverRunningState.Disconnected, false, false)]
    [InlineData(ObserverRunningState.Replaying, false, true)]
    [InlineData(ObserverRunningState.Quarantined, true, false)]
    [InlineData(ObserverRunningState.Active, true, false)]
    public async Task should_fail_closed(ObserverRunningState state, bool failed, bool replaying)
    {
        _progress.Position(Arg.Any<Type>()).Returns(Task.FromResult(new ObserverPosition(state, failed, replaying, new(5))));
        await Wait();
        _error.ShouldBeOfExactType<ReadModelsNotCaughtUp>();
    }
}
