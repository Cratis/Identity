// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_EventSourcedAccounts;

public class when_applying_the_distinct_issuer_vector : given.a_contract_vector
{
    AccountSubject _account;

    Task Establish() => Load("same-subject-different-issuers");

    async Task Because()
    {
        _account = await SignIn(VectorProof(new(Guid.Parse(Text(_when, "allocated_subject")))), "person@example.test", "Person", "display-only", new(Text(_when, "allocated_subject")));
        await Refresh();
    }

    [Fact] void should_allocate_a_different_account() => _account.Value.ShouldNotEqual(Text(_given.GetProperty("accounts")[0], "subject"));
    [Fact] void should_use_the_accepted_operation_subject() => _account.Value.ShouldEqual(Text(_then.GetProperty("account_key"), "subject"));
    [Fact] void should_keep_both_accounts() => _states.Instances.Count.ShouldEqual(_then.GetProperty("account_count").GetInt32());
    [Fact]
    void should_link_only_the_exact_issuer_pair()
    {
        var expected = _then.GetProperty("linked_keys")[0];
        var link = _states.InstanceForEventSourceId(_account.Value).Links.Values.Single();
        link.IdpIssuer.Value.ShouldEqual(Text(expected, "idp"));
        link.KeyHash.ShouldEqual(_digester.Digest(new(Text(expected, "idp")), new(Text(expected, "idp_sub"))));
    }
}
