// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.given;
using Cratis.Identity.Tenants;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_AccessTokenIdentity;

public class when_renewing : given.an_account_and_a_grant
{
    const string AuthorizationId = "authorization-1";
    ClaimsPrincipal _granted;
    ClaimsIdentity _identity;

    void Establish()
    {
        _granted = new ClaimsPrincipal(AccessTokenIdentity.Create(_account, ClientId, _grant, new SelectableTenant("cratis", "Cratis")));
        _granted.SetAuthorizationId(AuthorizationId);
    }

    void Because() => _identity = AccessTokenIdentity.Renew(_granted, _account with { GitHubLogin = "einari-renamed" }, ClientId);

    [Fact] void should_keep_the_tenant_of_the_grant() => _identity.GetClaim(IdentityClaims.TenantId).ShouldEqual("cratis");
    [Fact] void should_keep_the_authorization_the_grant_belongs_to() => _identity.GetAuthorizationId().ShouldEqual(AuthorizationId);
    [Fact] void should_keep_the_granted_scopes() => _identity.GetScopes().ShouldContainOnly("direct:read", "offline_access");
    [Fact] void should_keep_the_audience() => _identity.GetResources().ShouldContainOnly(Configurations.DirectAudience);
    [Fact] void should_refresh_the_github_login_from_the_account() => _identity.GetClaim(IdentityClaims.GitHubLogin).ShouldEqual("einari-renamed");
    [Fact] void should_have_a_single_subject() => _identity.FindAll("sub").Count().ShouldEqual(1);
}
