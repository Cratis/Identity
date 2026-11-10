// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_a_prepared_authorization_outlives_a_crash : given.an_activation
{
    bool _usableBefore;
    bool _usableAfter;

    async Task Because()
    {
        await Approve();
        store.StopBefore = ConsentRequestState.Active;
        try { await Resume(); } catch (InvalidOperationException) { }
        _usableBefore = await authorizations.Usable(request.Id, store, fence);
        activation = Restart();
        await Resume();
        _usableAfter = await authorizations.Usable(request.Id, store, fence);
    }

    [Fact] void should_not_expose_the_dormant_authorization() => _usableBefore.ShouldBeFalse();
    [Fact] void should_activate_after_restart() => _usableAfter.ShouldBeTrue();
    [Fact] void should_find_the_same_prepared_authorization() => authorizations.Count.ShouldEqual(1);
    [Fact] void should_not_append_duplicate_evidence() => evidence.Appends.ShouldEqual(1);
}
