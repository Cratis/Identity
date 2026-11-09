// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.RevocationSpecs;

public class when_recovering_a_lost_applied_tombstone : given.a_recovery_pass
{
    void Establish() => _contains = false;
    async Task Because() => await Run();

    [Fact] void should_replay_below_the_operational_checkpoint() => _applied.Single().Record.ShouldEqual(_record);
    [Fact] void should_allocate_a_recovery_epoch() => _epochs.Single().Epoch.ShouldEqual(1L);
    [Fact] void should_preserve_every_original_fact_and_detection_identity() => _applied.Single().Record.Intents.SequenceEqual(_record.Intents).ShouldBeTrue();
    [Fact] void should_not_claim_uncovered_restrictions_are_recovered() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_forbid_legacy_bootstrap() => _gate.AllowLegacyBootstrap.ShouldBeFalse();
    [Fact] void should_finish_without_an_unexpected_exception() => _failure.ShouldBeNull();
}
