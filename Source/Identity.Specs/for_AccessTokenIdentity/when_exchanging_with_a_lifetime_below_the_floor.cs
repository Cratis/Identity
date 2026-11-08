// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.given;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_AccessTokenIdentity;

public class when_exchanging_with_a_lifetime_below_the_floor : given.an_account_and_a_grant
{
    ClaimsIdentity _identity;

    void Because() => _identity = AccessTokenIdentity.Exchange(_account, "authproxy", _grant.Resource, "cratis", ["direct:read"], Configurations.ExternalIssuer, "s", null, TimeSpan.FromMilliseconds(10));

    [Fact] void should_floor_the_lifetime() => new ClaimsPrincipal(_identity).GetAccessTokenLifetime().ShouldEqual(AccessTokenIdentity.MinimumExchangeLifetime);
}
