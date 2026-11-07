// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_append_resolution_remains_unknown : given.an_activation
{
    ConsentRequest _unknown;
    ConsentRequest _resolved;
    string? _code;

    async Task Because()
    {
        await Approve();
        evidence.LoseAcknowledgement = true;
        await Resume();
        evidence.Unavailable = true;
        Expire();
        activation = Restart();
        _unknown = await Resume();
        _code = await Claim();
        evidence.Unavailable = false;
        _resolved = await Resume();
    }

    [Fact] void should_not_abandon_unknown_evidence() => _unknown.State.ShouldEqual(ConsentRequestState.Decided);
    [Fact] void should_not_expire_unresolved_work() => _unknown.PurgeAt.ShouldBeNull();
    [Fact] void should_not_issue_a_code() => _code.ShouldBeNull();
    [Fact] void should_resolve_on_a_later_retry_without_activating() => _resolved.State.ShouldEqual(ConsentRequestState.EvidenceWithheld);
    [Fact] void should_not_append_again() => evidence.Appends.ShouldEqual(1);
}
