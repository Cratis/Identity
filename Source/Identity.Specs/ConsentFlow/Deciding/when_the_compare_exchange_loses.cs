// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_the_compare_exchange_loses : given.an_activation
{
    ConsentRequest _after;

    async Task Because()
    {
        store.LoseNextCompareExchange = true;
        await Approve();
        store.LoseNextCompareExchange = true;
        _after = await Resume();
    }

    [Fact] void should_reload_and_resume() => _after.State.ShouldEqual(ConsentRequestState.Active);
    [Fact] void should_append_once_despite_a_retry() => evidence.Appends.ShouldEqual(1);
}
