// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_AccountSubject;

public class when_applying_the_malformed_subject_vector : given.a_contract_vector
{
    bool _accepted;

    Task Establish() => Load("malformed-subject-rejected");

    void Because() => _accepted = AccountSubject.TryParse(Text(_when.GetProperty("account_key"), "subject"), out _);

    [Fact] void should_not_treat_the_subject_as_an_account() => _accepted.ShouldBeFalse();
    [Fact] void should_not_normalize_the_subject() => _accepted.ShouldEqual(_then.GetProperty("subject_normalized").GetBoolean());
    [Fact] void should_not_allocate_an_account() => _states.Instances.ShouldBeEmpty();
}
