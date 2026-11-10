// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Linking.Proposing.when_proposing_a_link;

public class and_the_account_is_disabled_or_tombstoned : Accounts.given.a_link_proposal
{
    string _disabledSignIn;
    string _disabledLink;
    string _tombstonedSignIn;
    string _tombstonedLink;

    async Task Because()
    {
        await _events.EventLog.Append(_account.Value, new AccountDisablementRecorded("owner-disabled", 1, Guid.NewGuid(), 1, FactId.New()));
        await Refresh();
        _disabledSignIn = await Refusal(() => SignIn(Verified(GitHubIssuer, "existing-key", FactId.New()), "profile@example.test", "Person", "display"));
        _disabledLink = await Refusal(() => _linking.Propose(Session(_account), _proof, new(true)));
        await _events.EventLog.Append(_account.Value, new AccountTombstoneRecorded(_clock.Now, FactId.New()));
        await _events.EventLog.Append(_account.Value, new AccountEnablementRecorded(2, _clock.Now, FactId.New()));
        await Refresh();
        _tombstonedSignIn = await Refusal(() => SignIn(Verified(GitHubIssuer, "existing-key", FactId.New()), "profile@example.test", "Person", "display"));
        _tombstonedLink = await Refusal(() => _linking.Propose(Session(_account), _proof, new(true)));
    }

    [Fact] void should_refuse_disabled_sign_in() => _disabledSignIn.ShouldEqual("account_not_active");
    [Fact] void should_refuse_disabled_linking() => _disabledLink.ShouldEqual("account_not_active");
    [Fact] void should_refuse_tombstoned_sign_in_even_after_enablement() => _tombstonedSignIn.ShouldEqual("account_not_active");
    [Fact] void should_refuse_tombstoned_linking() => _tombstonedLink.ShouldEqual("account_not_active");
    [Fact] void should_keep_the_reservation() => _reservations.Instances.Values.Single().Keys.Count().ShouldEqual(1);
}
