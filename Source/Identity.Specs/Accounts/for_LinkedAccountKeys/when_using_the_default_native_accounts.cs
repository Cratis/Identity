// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_LinkedAccountKeys;

public class when_using_the_default_native_accounts : given.a_linked_keys_query
{
    Task Because() => Perform();

    [Fact] void should_return_the_native_account_keys() => Keys.ShouldNotBeEmpty();
    [Fact] void should_not_require_an_opt_in_switch() => _result.ExceptionMessages.ShouldBeEmpty();
}
