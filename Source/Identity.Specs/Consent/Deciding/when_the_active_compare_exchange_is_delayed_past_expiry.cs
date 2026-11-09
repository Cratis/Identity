// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_the_active_compare_exchange_is_delayed_past_expiry : given.an_activation
{
    ConsentRequest _after;
    bool _usable;
    string? _code;

    async Task Because()
    {
        await Approve();
        store.BeforeCompareExchange = (_, replacement) =>
        {
            if (replacement.State == ConsentRequestState.Active) Expire();
            return Task.CompletedTask;
        };
        _after = await Resume();
        _usable = await authorizations.Usable(request.Id, store, fence);
        _code = await Claim();
    }

    [Fact] void should_not_persist_active_after_the_deadline() => _after.State.ShouldEqual(ConsentRequestState.EvidenceWithheld);
    [Fact] void should_reject_the_expired_active_compare_exchange() => store.Attempts.Single(_ => _.Replacement.State == ConsentRequestState.Active).Won.ShouldBeFalse();
    [Fact] void should_never_expose_a_usable_grant() => _usable.ShouldBeFalse();
    [Fact] void should_never_issue_a_code() => _code.ShouldBeNull();
}
