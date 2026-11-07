// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_validating_operational_transitions : given.an_activation
{
    bool _graphMatches;
    bool _bindingChanged;
    bool _deadlineChanged;
    bool _revisionSkipped;
    bool _codeReclaimed;
    bool _missingRetention;

    void Because()
    {
        var allowed = new HashSet<(ConsentRequestState, ConsentRequestState)>
        {
            (ConsentRequestState.Pending, ConsentRequestState.Decided),
            (ConsentRequestState.Pending, ConsentRequestState.Denied),
            (ConsentRequestState.Pending, ConsentRequestState.Abandoned),
            (ConsentRequestState.Decided, ConsentRequestState.EvidenceRecorded),
            (ConsentRequestState.Decided, ConsentRequestState.Abandoned),
            (ConsentRequestState.EvidenceRecorded, ConsentRequestState.Active),
            (ConsentRequestState.EvidenceRecorded, ConsentRequestState.EvidenceWithheld),
            (ConsentRequestState.Active, ConsentRequestState.Active)
        };
        _graphMatches = Enum.GetValues<ConsentRequestState>().All(before => Enum.GetValues<ConsentRequestState>().All(after =>
        {
            var source = Snapshot(before, 0);
            var target = Snapshot(after, 1) with { CodeClaimed = before == ConsentRequestState.Active && after == ConsentRequestState.Active };
            return ConsentRequestTransitions.CanReplace(source, target) == allowed.Contains((before, after));
        }));
        var decision = Snapshot(ConsentRequestState.Decided, 1);
        _bindingChanged = ConsentRequestTransitions.CanReplace(request, decision with { Binding = binding with { Realm = "other" } });
        _deadlineChanged = ConsentRequestTransitions.CanReplace(request, decision with { Deadline = request.Deadline.AddMinutes(1) });
        _revisionSkipped = ConsentRequestTransitions.CanReplace(request, decision with { Revision = 2 });
        var claimed = Snapshot(ConsentRequestState.Active, 0) with { CodeClaimed = true };
        _codeReclaimed = ConsentRequestTransitions.CanReplace(claimed, claimed with { CodeClaimed = false, Revision = 1 });
        _missingRetention = ConsentRequestTransitions.CanReplace(request, Snapshot(ConsentRequestState.Abandoned, 1) with { PurgeAt = null });
    }

    ConsentRequest Snapshot(ConsentRequestState state, long revision) => request with
    {
        State = state,
        Revision = revision,
        AuthorizationId = state == ConsentRequestState.Active ? "authorization" : null,
        PurgeAt = state is ConsentRequestState.Denied or ConsentRequestState.Abandoned or ConsentRequestState.EvidenceWithheld ? clock.Now + options.TerminalRetention : null
    };

    [Fact] void should_accept_only_the_declared_transition_graph() => _graphMatches.ShouldBeTrue();
    [Fact] void should_reject_binding_changes() => _bindingChanged.ShouldBeFalse();
    [Fact] void should_reject_deadline_extensions() => _deadlineChanged.ShouldBeFalse();
    [Fact] void should_reject_skipped_revisions() => _revisionSkipped.ShouldBeFalse();
    [Fact] void should_never_restore_consumed_code_issuance() => _codeReclaimed.ShouldBeFalse();
    [Fact] void should_require_retention_for_safe_terminal_rows() => _missingRetention.ShouldBeFalse();
}
