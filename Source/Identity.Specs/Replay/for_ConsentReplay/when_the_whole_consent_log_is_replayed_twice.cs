// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Replay;

public class when_the_whole_consent_log_is_replayed_twice : given.a_replayed_activation
{
    string? _first;

    async Task Because()
    {
        await Approve();
        await Resume();
        _first = await Claim();
        await ReplayTwice();
    }

    [Fact] void should_allow_the_original_single_claim() => _first.ShouldNotBeNull();
    [Fact] void should_not_issue_a_second_code_after_replay() => code.ShouldBeNull();
    [Fact] void should_keep_one_prepared_authorization() => authorizations.Count.ShouldEqual(1);
    [Fact] void should_keep_one_evidence_fact() => evidence.Appends.ShouldEqual(1);
}
