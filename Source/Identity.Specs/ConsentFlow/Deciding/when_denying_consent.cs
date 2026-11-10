// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_denying_consent : Pending.given.a_consent_interaction
{
    ConsentRequest? _denied;
    Exception? _replayed;
    Exception? _approval;

    async Task Because()
    {
        await Deny();
        _denied = await store.Read(request.Id, CancellationToken.None);
        _replayed = await Record.ExceptionAsync(() => Deny());
        _approval = await Record.ExceptionAsync(() => Approve());
    }

    [Fact] void should_deny_without_selection() => _denied.Selection.ShouldBeNull();
    [Fact] void should_be_terminal() => _denied.State.ShouldEqual(ConsentRequestState.Denied);
    [Fact] void should_retain_only_for_the_configured_period() => _denied.PurgeAt.ShouldEqual(clock.Now + options.TerminalRetention);
    [Fact] void should_refuse_replay() => _replayed.ShouldBeOfExactType<ConsentDecisionRefused>();
    [Fact] void should_refuse_approval_after_denial() => _approval.ShouldBeOfExactType<ConsentDecisionRefused>();
    [Fact] void should_not_issue_or_activate_anything() => authorizations.Count.ShouldEqual(0);
    [Fact] void should_not_write_evidence() => evidence.Appends.ShouldEqual(0);
}
