// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.for_ReadModelCatchUp;

public class when_both_projections_have_reached_their_own_targets : given.projection_progress
{
    Task Because() => Wait();

    [Fact] void should_allow_the_retry() => _error.ShouldBeNull();
    [Fact] Task should_capture_each_target_once() => _progress.Received(2).Tail(Arg.Any<Type>());
    [Fact] Task should_inspect_both_projections() => _progress.Received(2).Position(Arg.Any<Type>());
}
