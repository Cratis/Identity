// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Pending;

public class when_finding_pending_requests : Deciding.given.an_activation
{
    ConsentRequest? _found;
    ConsentRequest? _otherAccount;
    ConsentRequest? _otherSession;
    ConsentRequest? _otherRealm;
    ConsentRequest? _expired;
    ConsentRequest _newest;

    async Task Because()
    {
        clock.Now += TimeSpan.FromSeconds(1);
        _newest = await activation.Create(binding, CancellationToken.None);
        await activation.Create(binding with { Session = "other-session" }, CancellationToken.None);
        _found = await store.FindPending(binding.Account, binding.Session, binding.Realm, clock.Now, CancellationToken.None);
        _otherAccount = await store.FindPending("00000000-0000-4000-8000-000000000099", binding.Session, binding.Realm, clock.Now, CancellationToken.None);
        _otherSession = await store.FindPending(binding.Account, "unknown-session", binding.Realm, clock.Now, CancellationToken.None);
        _otherRealm = await store.FindPending(binding.Account, binding.Session, "other-realm", clock.Now, CancellationToken.None);
        clock.Now = _newest.Deadline;
        _expired = await store.FindPending(binding.Account, binding.Session, binding.Realm, clock.Now, CancellationToken.None);
    }

    [Fact] void should_return_the_newest_created_request() => _found.ShouldEqual(_newest);
    [Fact] void should_not_cross_accounts() => _otherAccount.ShouldBeNull();
    [Fact] void should_not_cross_sessions() => _otherSession.ShouldBeNull();
    [Fact] void should_not_cross_realms() => _otherRealm.ShouldBeNull();
    [Fact] void should_exclude_the_deadline_boundary() => _expired.ShouldBeNull();
    [Fact] void should_set_created_at_from_the_clock() => _newest.CreatedAt.ShouldEqual(request.CreatedAt.AddSeconds(1));
}
