// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_LinkedAccountKeys;

public class when_listing_the_current_accounts_keys : given.a_linked_keys_query
{
    Task Because() => Perform();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_no_exceptions() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_only_return_the_callers_links_and_proposals() => Keys.Count().ShouldEqual(2);
    [Fact] void should_include_the_pending_state() => Keys.Count(key => key.State == "pending_proof" && key.LinkedAt is null).ShouldEqual(1);
    [Fact] void should_include_the_confirmed_state() => Keys.Count(key => key.State == "confirmed" && key.LinkedAt is not null).ShouldEqual(1);
}
