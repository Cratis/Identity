// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_a_restore_follows_an_interrupted_consent_decision : given.an_activation
{
    bool _interrupted;
    ConsentRequest? _afterInterruption;
    ConsentRequest? _decided;
    ConsentRequest? _restored;

    async Task Because()
    {
        _decided = await Approve();
        evidence.BeforeRecord = () =>
        {
            _interrupted = true;
            throw new InvalidOperationException("Interrupted before evidence append");
        };
        _afterInterruption = await Resume();
        var snapshot = new given.MemoryRequests { Clock = clock };
        await snapshot.Create(request, CancellationToken.None);
        gate.ObserveEpoch(1);
        gate.Open(true);
        activation = new(snapshot, evidence, fence, authorizations, clock, Options.Create(options), gate);
        _restored = await Resume();
    }

    [Fact] void should_have_committed_the_decision_before_the_interruption() => _decided.State.ShouldEqual(ConsentRequestState.Decided);
    [Fact] void should_interrupt_the_evidence_append() => _interrupted.ShouldBeTrue();
    [Fact] void should_leave_the_decision_unresolved_after_the_interruption() => _afterInterruption.State.ShouldEqual(ConsentRequestState.Decided);
    [Fact] void should_deny_the_restored_pending_request() => _restored.ShouldBeNull();
    [Fact] void should_not_append_evidence_for_the_restored_request() => evidence.Appends.ShouldEqual(0);
    [Fact] void should_not_prepare_an_authorization() => authorizations.Count.ShouldEqual(0);
}
