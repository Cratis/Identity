// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_a_pending_request_predates_a_restore : given.an_activation
{
    ConsentRequest _pending;
    ConsentRequest? _decided;
    ConsentRequest? _resumed;
    ConsentRequest? _current;

    async Task Because()
    {
        _pending = await activation.Create(binding, CancellationToken.None);
        gate.ObserveEpoch(1);
        gate.Open(true);
        _decided = await activation.Decide(_pending.Id, binding, true, CancellationToken.None);
        _resumed = await activation.Resume(_pending.Id, CancellationToken.None);
        var fresh = await activation.Create(binding, CancellationToken.None);
        await activation.Decide(fresh.Id, binding, true, CancellationToken.None);
        _current = await activation.Resume(fresh.Id, CancellationToken.None);
    }

    [Fact] void should_deny_the_pre_restore_decision() => _decided.ShouldBeNull();
    [Fact] void should_deny_pre_restore_reconciliation() => _resumed.ShouldBeNull();
    [Fact] void should_not_rewrite_the_old_creation_epoch() => _pending.RecoveryEpoch.ShouldEqual(0);
    [Fact] void should_activate_only_the_fresh_request() => _current.State.ShouldEqual(ConsentRequestState.Active);
    [Fact] void should_stamp_the_new_epoch() => _current.RecoveryEpoch.ShouldEqual(1);
    [Fact] void should_append_only_fresh_evidence() => evidence.Appends.ShouldEqual(1);
}
