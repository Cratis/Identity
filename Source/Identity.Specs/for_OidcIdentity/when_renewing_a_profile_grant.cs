// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_OidcIdentity;

public class when_renewing_a_profile_grant : given.an_identity_grant
{
    ClaimsIdentity _identity;

    void Because()
    {
        var original = OidcIdentity.Create(new(_account, AuthTime, SessionId), ClientId, new IdentityOnlyGrant(["openid", "profile", "offline_access"]), "example-nonce");
        original.SetAuthorizationId("example-authorization");
        _identity = OidcIdentity.Renew(new ClaimsPrincipal(original), _account with { Name = "Updated Member", GitHubLogin = "updated-member" }, ClientId);
    }

    [Fact] void should_keep_the_original_authentication_time() => _identity.GetClaim(Claims.AuthenticationTime).ShouldEqual("1700000000");
    [Fact] void should_keep_the_original_nonce() => _identity.GetClaim(Claims.Nonce).ShouldEqual("example-nonce");
    [Fact] void should_keep_the_original_session() => _identity.GetClaim(OidcIdentity.SessionIdClaim).ShouldEqual(SessionId);
    [Fact] void should_keep_the_authorization_binding() => _identity.GetAuthorizationId().ShouldEqual("example-authorization");
    [Fact] void should_keep_the_scope_ceiling() => _identity.GetScopes().ShouldContainOnly("openid", "profile", "offline_access");
    [Fact] void should_keep_the_userinfo_binding() => _identity.GetResources().ShouldContainOnly(OidcIdentity.UserInfoAudience);
    [Fact] void should_refresh_the_name() => _identity.GetClaim(Claims.Name).ShouldEqual("Updated Member");
    [Fact] void should_refresh_preferred_username() => _identity.GetClaim(Claims.PreferredUsername).ShouldEqual("updated-member");
    [Fact] void should_not_send_profile_to_the_access_token() => _identity.Claims.Where(claim => claim.HasDestination(Destinations.AccessToken)).Select(claim => claim.Type).ShouldContainOnly(Claims.Subject);
    [Fact] void should_remain_identity_only() => OidcIdentity.IsIdentityOnly(new ClaimsPrincipal(_identity)).ShouldBeTrue();
}
