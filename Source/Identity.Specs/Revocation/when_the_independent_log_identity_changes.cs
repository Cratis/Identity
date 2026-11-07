// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_the_independent_log_identity_changes : given.a_recovery_pass
{
    void Establish() => _custody = _custody with { InstanceId = "replacement-log" };
    async Task Because() => await Run();

    [Fact] void should_refuse_to_rebind_history() => _failure.ShouldBeOfExactType<FenceLogConflict>();
    [Fact] void should_remain_unready() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_not_overwrite_the_operational_binding() => _operational.DidNotReceive().Save(Arg.Any<RecoveryCheckpoint?>(), Arg.Any<RecoveryCheckpoint>(), Arg.Any<CancellationToken>());
}
