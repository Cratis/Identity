// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_AccessTokenIdentity;

public class when_reading_the_authentication_time_of_a_grant_without_one : Specification
{
    long? _result;

    void Because() => _result = AccessTokenIdentity.AuthTimeOf(new ClaimsPrincipal(new ClaimsIdentity("test")));

    [Fact] void should_not_invent_one() => _result.ShouldBeNull();
}
