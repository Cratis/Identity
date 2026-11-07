// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_the_append_acknowledgement_is_lost_before_expiry : given.an_activation
{
    ConsentRequest _unknown;
    ConsentRequest _resolved;
    string? _code;

    async Task Because()
    {
        await Approve();
        evidence.LoseAcknowledgement = true;
        _unknown = await Resume();
        Expire();
        activation = Restart();
        _resolved = await Resume();
        _code = await Claim();
    }

    [Fact] void should_preserve_the_uncertain_decision() => _unknown.State.ShouldEqual(ConsentRequestState.Decided);
    [Fact] void should_resolve_to_a_distinct_safe_terminal_state() => _resolved.State.ShouldEqual(ConsentRequestState.EvidenceWithheld);
    [Fact] void should_not_misrepresent_existing_evidence_as_absent() => evidence.Appends.ShouldEqual(1);
    [Fact] void should_issue_no_code() => _code.ShouldBeNull();
    [Fact] void should_prepare_no_authorization() => authorizations.Count.ShouldEqual(0);
    [Fact] void should_retain_the_safe_terminal_decision_for_thirty_days() => _resolved.PurgeAt.ShouldEqual(clock.Now + options.TerminalRetention);
}
