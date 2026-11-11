// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_OidcIdentity;

public class when_creating_a_profile_grant : given.an_identity_grant
{
    ClaimsIdentity _identity;

    void Because() => _identity = OidcIdentity.Create(new(_account, AuthTime, SessionId), ClientId, new IdentityOnlyGrant(["openid", "profile"]));

    [Fact] void should_use_the_canonical_subject() => _identity.GetClaim(Claims.Subject).ShouldEqual(_account.Id.Value);
    [Fact] void should_bind_only_to_userinfo() => _identity.GetResources().ShouldContainOnly(OidcIdentity.UserInfoAudience);
    [Fact] void should_preserve_the_scopes() => _identity.GetScopes().ShouldContainOnly("openid", "profile");
    [Fact] void should_send_only_the_subject_to_the_access_token() => _identity.Claims.Where(claim => claim.HasDestination(Destinations.AccessToken)).Select(claim => claim.Type).ShouldContainOnly(Claims.Subject);
    [Fact] void should_send_only_the_profile_to_the_identity_token() => _identity.FindFirst(Claims.Name).GetDestinations().ShouldContainOnly(Destinations.IdentityToken);
    [Fact] void should_send_preferred_username_only_to_the_identity_token() => _identity.FindFirst(Claims.PreferredUsername).GetDestinations().ShouldContainOnly(Destinations.IdentityToken);
    [Fact] void should_preserve_the_original_authentication_time() => _identity.GetClaim(Claims.AuthenticationTime).ShouldEqual("1700000000");
    [Fact] void should_encode_auth_time_as_an_integer() => _identity.FindFirst(Claims.AuthenticationTime).ValueType.ShouldEqual(ClaimValueTypes.Integer64);
    [Fact] void should_send_auth_time_only_to_the_identity_token() => _identity.FindFirst(Claims.AuthenticationTime).GetDestinations().ShouldContainOnly(Destinations.IdentityToken);
    [Fact] void should_carry_basic_assurance() => _identity.GetClaim(Claims.AuthenticationContextReference).ShouldEqual("basic");
    [Fact] void should_preserve_the_site_session() => _identity.GetClaim(OidcIdentity.SessionIdClaim).ShouldEqual(SessionId);
    [Fact] void should_send_sid_only_to_the_identity_token() => _identity.FindFirst(OidcIdentity.SessionIdClaim).GetDestinations().ShouldContainOnly(Destinations.IdentityToken);
    [Fact] void should_not_add_a_tenant() => _identity.GetClaim(IdentityClaims.TenantId).ShouldBeNull();
    [Fact] void should_not_add_upstream_identity() => _identity.GetClaim(IdentityClaims.IdpSub).ShouldBeNull();
    [Fact] void should_not_add_github_identity() => _identity.GetClaim(IdentityClaims.GitHubId).ShouldBeNull();
    [Fact] void should_keep_the_grant_discriminator_private() => _identity.FindFirst(OidcIdentity.GrantMarker).GetDestinations().ShouldBeEmpty();
}
