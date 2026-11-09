// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_the_request_expires_before_activation : given.an_activation
{
    ConsentRequest _after;
    ConsentEvidenceResolution _lateAppend;

    async Task Because()
    {
        await Approve();
        Expire();
        _after = await Resume();
        _lateAppend = await evidence.Record(request, CancellationToken.None);
    }

    [Fact] void should_abandon_only_after_authoritative_absence() => _after.State.ShouldEqual(ConsentRequestState.Abandoned);
    [Fact] void should_append_no_fact() => evidence.Appends.ShouldEqual(0);
    [Fact] void should_seal_late_writers() => _lateAppend.ShouldEqual(ConsentEvidenceResolution.NotRecorded);
    [Fact] void should_prepare_no_authorization() => authorizations.Count.ShouldEqual(0);
    [Fact] void should_set_terminal_retention() => _after.PurgeAt.ShouldEqual(clock.Now + options.TerminalRetention);
}
