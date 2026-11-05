// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Identity.Accounts.for_LinkedAccountKeys;

public class when_the_account_subject_is_malformed : given.a_linked_keys_query
{
    protected override ClaimsPrincipal Principal => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "not-an-account")], "session"));

    Task Because() => Perform();

    [Fact] void should_succeed_without_account_data() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_leak_another_accounts_keys() => Keys.ShouldBeEmpty();
}
