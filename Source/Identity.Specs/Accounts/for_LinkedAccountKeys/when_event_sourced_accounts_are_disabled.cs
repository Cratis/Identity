// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_LinkedAccountKeys;

public class when_event_sourced_accounts_are_disabled : given.a_linked_keys_query
{
    protected override bool Enabled => false;

    Task Because() => Perform();

    [Fact] void should_return_no_event_sourced_data() => _result.Data.ShouldBeNull();
    [Fact] void should_enforce_the_opt_in_boundary() => _result.ExceptionMessages.ShouldContain("pending owner decision D18");
}
