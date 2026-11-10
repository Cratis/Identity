// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.Proposing;

public class when_applying_the_email_only_proof_vector : Accounts.given.a_contract_vector
{
    AccountOperationRefused _refusal;

    Task Establish() => Load("email-link-refused");

    async Task Because()
    {
        try
        {
            await _linking.Propose(Session(new(Text(_when.GetProperty("target_account"), "subject"))), VectorProof(FactId.New()), new(false, true));
        }
        catch (AccountOperationRefused exception)
        {
            _refusal = exception;
        }

        await Refresh();
    }

    [Fact] void should_refuse_email_as_proof() => _refusal.Message.ShouldEqual(Text(_then, "reason"));
    [Fact] void should_create_no_account() => _states.Instances.Count.ShouldEqual(_given.GetProperty("accounts").GetArrayLength());
    [Fact] void should_not_create_a_proposal() => _states.Instances.Values.Single().Proposals.ShouldBeEmpty();
    [Fact] void should_not_link_by_matching_email() => _states.Instances.Values.Single().Links.Count.ShouldEqual(1);
}
