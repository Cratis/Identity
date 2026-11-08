// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts;
using Cratis.Identity.given;
using Cratis.Identity.Tenants;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_AccessTokenIdentity;

public class when_renewing : given.an_account_and_a_grant
{
    const string AuthorizationId = "authorization-1";
    ClaimsPrincipal _granted;
    ClaimsIdentity _identity;

    void Establish()
    {
        _granted = new ClaimsPrincipal(AccessTokenIdentity.Create(_account, ClientId, _grant, new SelectableTenant("cratis", "Cratis"), AuthTime));
        _granted.SetAuthorizationId(AuthorizationId);
    }

    void Because() => _identity = AccessTokenIdentity.Renew(_granted, _account with { GitHubLogin = "einari-renamed", GitHubId = "999" }, ClientId, true);

    [Fact] void should_keep_the_tenant_of_the_grant() => _identity.GetClaim(IdentityClaims.TenantId).ShouldEqual("cratis");
    [Fact] void should_keep_the_authorization_the_grant_belongs_to() => _identity.GetAuthorizationId().ShouldEqual(AuthorizationId);
    [Fact] void should_keep_the_granted_scopes() => _identity.GetScopes().ShouldContainOnly("direct:read", "offline_access");
    [Fact] void should_keep_the_audience() => _identity.GetResources().ShouldContainOnly(Configurations.DirectAudience);
    [Fact] void should_refresh_the_github_login_from_the_account() => _identity.GetClaim(IdentityClaims.GitHubLogin).ShouldEqual("einari-renamed");
    [Fact] void should_refresh_the_idp_sub_from_the_account() => _identity.GetClaim(IdentityClaims.IdpSub).ShouldEqual("999");
    [Fact] void should_refresh_the_idp() => _identity.GetClaim(IdentityClaims.Idp).ShouldEqual("https://github.com");
    [Fact] void should_have_a_single_idp() => _identity.FindAll(IdentityClaims.Idp).Count().ShouldEqual(1);
    [Fact] void should_send_idp_only_to_the_access_token() => _identity.FindFirst(IdentityClaims.Idp).GetDestinations().ShouldContainOnly("access_token");
    [Fact] void should_send_idp_sub_only_to_the_access_token() => _identity.FindFirst(IdentityClaims.IdpSub).GetDestinations().ShouldContainOnly("access_token");
    [Fact] void should_keep_the_original_authentication_time() => _identity.GetClaim(Claims.AuthenticationTime).ShouldEqual("1700000000");
    [Fact] void should_have_a_single_authentication_time() => _identity.FindAll(Claims.AuthenticationTime).Count().ShouldEqual(1);
    [Fact] void should_keep_the_assurance() => _identity.GetClaim(Claims.AuthenticationContextReference).ShouldEqual("basic");
    [Fact] void should_refresh_the_github_id_for_a_consumer() => _identity.GetClaim(IdentityClaims.GitHubId).ShouldEqual("999");
    [Fact] void should_have_a_single_subject() => _identity.FindAll("sub").Count().ShouldEqual(1);
}
