// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_EventSourcedAccounts;

public class when_applying_the_github_login_rename_vector : given.a_contract_vector
{
    AccountSubject _account;
    AccountLinkState _state;
    Guid _originalLink;

    async Task Establish()
    {
        await Load("github-login-rename");
        _originalLink = _states.Instances.Values.Single().Links.Keys.Single();
    }

    async Task Because()
    {
        _account = await SignIn(VectorProof(FactId.New()), "person@example.test", "Person", new(Text(_when.GetProperty("profile"), "login")));
        await Refresh();
        _state = _states.InstanceForEventSourceId(_account.Value);
    }

    [Fact] void should_keep_the_account() => _account.Value.ShouldEqual(Text(_then.GetProperty("account_key"), "subject"));
    [Fact] void should_keep_the_account_count() => _states.Instances.Count.ShouldEqual(_then.GetProperty("account_count").GetInt32());
    [Fact] void should_observe_the_new_login() => _state.Logins.Values.Single().Value.ShouldEqual(Text(_then.GetProperty("profile"), "login"));
    [Fact] void should_keep_the_confirmed_key() => _state.Links.Keys.Single().ShouldEqual(_originalLink);
}
