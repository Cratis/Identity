// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.RevocationSpecs;

public class when_replaying_an_uncheckpointed_tail : given.a_recovery_pass
{
    void Establish()
    {
        _contains = false;
        _checkpoint = _checkpoint with { AppliedThrough = 0 };
        _custody = _custody with { AppliedThrough = 0 };
    }

    async Task Because() => await Run();

    [Fact] void should_replay_the_tail() => _applied.Single().Record.ShouldEqual(_record);
    [Fact] void should_advance_both_progress_markers() => _checkpoint.AppliedThrough.ShouldEqual(_custody.AppliedThrough);
    [Fact] void should_reach_the_contiguous_watermark() => _checkpoint.AppliedThrough.ShouldEqual(1L);
    [Fact] void should_not_mistake_new_work_for_a_restore() => _epochs.Count.ShouldEqual(0);
    [Fact] void should_admit_only_after_reconciliation() => _gate.Ready.ShouldBeTrue();
}
