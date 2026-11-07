// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_an_operational_restore_is_confirmed_without_chronicle : given.a_recovery_pass
{
    void Establish()
    {
        _contains = false;
        _session.Bound = false;
        _facts.Bind(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new TimeoutException()));
        _options.RestoreKind = RestoreKind.Operational;
        _options.RestoreId = "operational-restore-test";
        _options.RestrictionsReestablished = true;
        _facts.Publish(Arg.Any<RecoveryEpochIntent>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new TimeoutException()));
    }

    async Task Because() => await Run();

    [Fact] void should_reconstruct_the_permanent_denial() => _applied.Single().Record.ShouldEqual(_record);
    [Fact] void should_not_require_chronicle_for_confirmed_operational_coverage() => _gate.Ready.ShouldBeTrue();
    [Fact] void should_leave_a_durable_epoch_intent_for_later_delivery() => _epochs.Count.ShouldEqual(1);
    [Fact] void should_not_regress_the_epoch() => _gate.Epoch.ShouldEqual(1L);
    [Fact] void should_not_bind_chronicle_before_repair() => _facts.DidNotReceive().Bind(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
