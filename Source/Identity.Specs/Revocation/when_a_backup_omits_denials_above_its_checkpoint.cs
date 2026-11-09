// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.RevocationSpecs;

public class when_a_backup_omits_denials_above_its_checkpoint : given.a_recovery_pass
{
    void Establish()
    {
        _checkpoint = _checkpoint with { AppliedThrough = 0 };
        _custody = _custody with { AppliedThrough = 0 };
        _contains = false;
    }

    async Task Because() => await Run();

    [Fact] void should_detect_a_restore_even_when_both_progress_markers_match() => _epochs.Count.ShouldEqual(1);
    [Fact] void should_repair_the_denial_above_the_backup_checkpoint() => _applied.Single().Record.ShouldEqual(_record);
    [Fact] void should_cover_the_full_contiguous_log() => _checkpoint.AppliedThrough.ShouldEqual(_watermark);
    [Fact] void should_keep_uncovered_restriction_classes_denied() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_forbid_legacy_adoption() => _gate.AllowLegacyBootstrap.ShouldBeFalse();
}
