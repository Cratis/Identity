// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Linking.Confirming.when_confirming_a_link;

public class and_building_the_atomic_batch : given.a_confirmation_decision
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_record_clearance_and_link_facts_together(bool clearing)
    {
        SetState(clearing ? "quarantined" : "pending_proof");
        var (events, outcome) = Handle(clearing).AsT0;
        var batch = events.ToArray();
        var expected = clearing
            ? new[] { typeof(UpstreamKeyLinkCleared), typeof(UpstreamKeyLinkRecorded), typeof(UpstreamKeySubjectRecorded) }
            : [typeof(UpstreamKeyLinkRecorded), typeof(UpstreamKeySubjectRecorded)];
        batch.Select(item => item.Event.GetType()).ShouldEqual(expected);
        batch.All(item => item.EventSourceId.Value == _account.Value).ShouldBeTrue();
        batch.All(item => item.Occurred == _clock.Now).ShouldBeTrue();
        outcome.LinkId.Value.ShouldEqual(_proposalId.Value);
        var link = batch.Select(item => item.Event).OfType<UpstreamKeyLinkRecorded>().Single();
        link.EvidenceId.Value.ShouldEqual(_decision.Authentication.OperationId.Value);
        link.LinkedAt.ShouldEqual(_decision.Authentication.AuthTime);
        link.FactId.ShouldEqual(FactId.ForOperation(new(_proposalId.Value), nameof(UpstreamKeyLinkRecorded)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_quarantine_without_clearance_or_link_facts_when_reserved(bool clearing)
    {
        SetState(clearing ? "quarantined" : "pending_proof");
        _decision = _decision with { Reserved = true };
        var (events, outcome) = Handle(clearing).AsT0;
        events.Select(item => item.Event.GetType()).ShouldEqual([typeof(UpstreamKeyLinkQuarantined)]);
        outcome.LinkId.ShouldBeNull();
    }
}
