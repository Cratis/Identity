// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_activation_expires_during_preparation : given.an_activation
{
    ConsentRequest _after;
    bool _usable;

    async Task Because()
    {
        await Approve();
        authorizations.BeforePrepare = Expire;
        _after = await Resume();
        _usable = await authorizations.Usable(request.Id, store, fence);
    }

    [Fact] void should_not_activate_after_the_deadline() => _after.State.ShouldEqual(ConsentRequestState.EvidenceWithheld);
    [Fact] void should_keep_the_prepared_authorization_unusable() => _usable.ShouldBeFalse();
}
