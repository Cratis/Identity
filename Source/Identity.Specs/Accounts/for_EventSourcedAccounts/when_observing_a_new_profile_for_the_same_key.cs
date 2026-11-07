// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.for_EventSourcedAccounts;

public class when_observing_a_new_profile_for_the_same_key : given.an_account_store
{
    AccountSubject _first;
    AccountSubject _second;
    AccountLinkState _state;
    string _preferredUsername;

    async Task Because()
    {
        _first = await _accounts.SignIn(GitHub(12345), "before@example.test", "Person", "old-login");
        await Refresh();
        _second = await _accounts.SignIn(GitHub(12345), "after@example.test", "Person", "new-login");
        await Refresh();
        _state = _states.InstanceForEventSourceId(_first.Value);
        _preferredUsername = EventSourcedAccountPrincipal.For(_first, _state.Logins.Values.Single(), _clock).FindFirst("preferred_username").Value;
    }

    [Fact] void should_keep_the_immutable_subject() => _second.ShouldEqual(_first);
    [Fact] void should_keep_one_account() => _states.Instances.Count.ShouldEqual(1);
    [Fact] void should_keep_one_link() => _state.Links.Count.ShouldEqual(1);
    [Fact] void should_update_the_display_identifier() => _state.Logins.Values.Single().Value.ShouldEqual("new-login");
    [Fact] void should_update_email_without_matching_on_it() => _state.Email.Value.ShouldEqual("after@example.test");
    [Fact] void should_source_preferred_username_from_the_observed_login() => _preferredUsername.ShouldEqual("new-login");
}
