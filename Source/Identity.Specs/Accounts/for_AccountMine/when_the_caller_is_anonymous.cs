// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Identity.Accounts.for_AccountMine;

public class when_the_caller_is_anonymous : given.an_owner_query
{
    protected override ClaimsPrincipal Principal => new();

    Task Because() => Perform();

    [Fact] void should_refuse_the_query() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_read_a_profile() => _reader.ReceivedCalls().ShouldBeEmpty();
}
