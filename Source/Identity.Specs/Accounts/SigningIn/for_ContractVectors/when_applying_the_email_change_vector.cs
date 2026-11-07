// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.SigningIn.for_ContractVectors;

public class when_applying_the_email_change_vector : Accounts.given.a_contract_vector
{
    AccountSubject _account;

    Task Establish() => Load("email-change");

    async Task Because()
    {
        _account = await SignIn(VectorProof(FactId.New()), new(Text(_when.GetProperty("profile"), "email")), "Person", "display-only");
        await Refresh();
    }

    [Fact] void should_keep_the_account() => _account.Value.ShouldEqual(Text(_then.GetProperty("account_key"), "subject"));
    [Fact] void should_keep_one_account() => _states.Instances.Count.ShouldEqual(_then.GetProperty("account_count").GetInt32());
    [Fact] void should_update_only_profile_email() => _states.InstanceForEventSourceId(_account.Value).Email.Value.ShouldEqual(Text(_then.GetProperty("profile"), "email"));
    [Fact] void should_not_link_another_key() => _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(1);
}
