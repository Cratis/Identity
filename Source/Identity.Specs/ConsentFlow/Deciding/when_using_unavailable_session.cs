// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_using_unavailable_session : Pending.given.a_consent_interaction
{
    Pending.PendingConsent? _pending;
    Exception? _approval;
    Exception? _denial;

    async Task Because()
    {
        sessions.SessionId.Returns(new UnavailableConsentSession().SessionId);
        _pending = await interaction.Find(CancellationToken.None);
        _approval = await Record.ExceptionAsync(() => Approve());
        _denial = await Record.ExceptionAsync(() => Deny());
    }

    [Fact] void should_not_display_consent() => _pending.ShouldBeNull();
    [Fact] void should_refuse_approval() => _approval.ShouldBeOfExactType<ConsentDecisionRefused>();
    [Fact] void should_refuse_denial() => _denial.ShouldBeOfExactType<ConsentDecisionRefused>();
}
