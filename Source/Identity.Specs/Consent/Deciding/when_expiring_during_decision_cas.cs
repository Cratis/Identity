// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_expiring_during_decision_cas : Pending.given.a_consent_interaction
{
    Exception? _refused;
    ConsentRequest? _after;

    async Task Because()
    {
        store.BeforeCompareExchange = (_, _) =>
        {
            clock.Now = request.Deadline;
            return Task.CompletedTask;
        };
        _refused = await Record.ExceptionAsync(() => Approve());
        _after = await store.Read(request.Id, CancellationToken.None);
    }

    [Fact] void should_refuse_the_stale_write() => _refused.ShouldBeOfExactType<ConsentDecisionRefused>();
    [Fact] void should_preserve_pending_state() => _after.ShouldEqual(request);
    [Fact] void should_not_write_evidence() => evidence.Appends.ShouldEqual(0);
}
