// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Identity.Accounts.for_LinkedAccountKeys;

public class when_the_caller_is_anonymous : given.a_linked_keys_query
{
    protected override ClaimsPrincipal Principal => new();

    Task Because() => Perform();

    [Fact] void should_require_authentication() => _result.IsAuthorized.ShouldBeFalse();
}
