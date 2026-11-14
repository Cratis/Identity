// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_AccessTokenIdentity;

public class when_exchanging_a_github_subject_token : given.an_account_and_a_grant
{
    ClaimsIdentity _identity;

    void Because() => _identity = AccessTokenIdentity.Exchange(_account, "authproxy", _grant.Resource, "cratis", ["direct:read"], "https://github.com", "12345", null, TimeSpan.FromMinutes(5));

    [Fact] void should_carry_the_github_login_as_preferred_username() => _identity.GetClaim(Claims.PreferredUsername).ShouldEqual(_account.DisplayLogin().Value);
    [Fact] void should_name_github_as_idp() => _identity.GetClaim(IdentityClaims.Idp).ShouldEqual("https://github.com");
}
