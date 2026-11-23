// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts;
using Cratis.Identity.Tenants;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_AccessTokenIdentity;

public class when_renewing_for_a_resource_that_no_longer_receives_upstream_identity : given.an_account_and_a_grant
{
    ClaimsIdentity _identity;

    void Because()
    {
        var granted = new ClaimsPrincipal(AccessTokenIdentity.Create(_account, ClientId, _grant, new SelectableTenant("cratis", "Cratis"), AuthTime, _account.LinkedUpstreamKeys.Single().LinkId.Value));
        _identity = AccessTokenIdentity.Renew(granted, _account, ClientId, false);
    }

    [Fact] void should_drop_the_idp_sub() => _identity.FindFirst(IdentityClaims.IdpSub).ShouldBeNull();
    [Fact] void should_drop_the_github_id() => _identity.FindFirst(IdentityClaims.GitHubId).ShouldBeNull();
    [Fact] void should_drop_the_github_login() => _identity.FindFirst(IdentityClaims.GitHubLogin).ShouldBeNull();
    [Fact] void should_drop_the_preferred_username() => _identity.FindFirst(Claims.PreferredUsername).ShouldBeNull();
    [Fact] void should_keep_the_display_name_as_name() => _identity.GetClaim(Claims.Name).ShouldEqual("Einar Ingebrigtsen");
    [Fact] void should_keep_the_subject() => _identity.GetClaim(Claims.Subject).ShouldEqual(_account.Id.Value);
    [Fact] void should_keep_the_idp() => _identity.GetClaim(IdentityClaims.Idp).ShouldEqual("https://github.com");
    [Fact] void should_keep_the_original_authentication_time() => _identity.GetClaim("auth_time").ShouldEqual("1700000000");
}
