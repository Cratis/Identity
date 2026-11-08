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

public class when_creating : given.an_account_and_a_grant
{
    ClaimsIdentity _identity;

    void Because() => _identity = AccessTokenIdentity.Create(_account, ClientId, _grant, new SelectableTenant("cratis", "Cratis"), AuthTime);

    [Fact] void should_carry_the_upstream_authentication_time() => _identity.GetClaim(Claims.AuthenticationTime).ShouldEqual("1700000000");
    [Fact] void should_carry_the_authentication_time_as_an_integer() => _identity.FindFirst(Claims.AuthenticationTime).ValueType.ShouldEqual(ClaimValueTypes.Integer64);
    [Fact] void should_carry_basic_assurance() => _identity.GetClaim(Claims.AuthenticationContextReference).ShouldEqual("basic");
    [Fact] void should_send_auth_time_only_to_the_access_token() => _identity.FindFirst(Claims.AuthenticationTime).GetDestinations().ShouldContainOnly("access_token");
    [Fact] void should_send_acr_only_to_the_access_token() => _identity.FindFirst(Claims.AuthenticationContextReference).GetDestinations().ShouldContainOnly("access_token");
    [Fact] void should_use_the_canonical_account_id_as_subject() => _identity.GetClaim("sub").ShouldEqual(_account.Id.Value);
    [Fact] void should_carry_the_github_id() => _identity.GetClaim(IdentityClaims.GitHubId).ShouldEqual("134365");
    [Fact] void should_carry_the_github_issuer_as_idp() => _identity.GetClaim(IdentityClaims.Idp).ShouldEqual("https://github.com");
    [Fact] void should_carry_the_github_id_as_idp_sub() => _identity.GetClaim(IdentityClaims.IdpSub).ShouldEqual("134365");
    [Fact] void should_send_idp_only_to_the_access_token() => _identity.FindFirst(IdentityClaims.Idp).GetDestinations().ShouldContainOnly("access_token");
    [Fact] void should_send_idp_sub_only_to_the_access_token() => _identity.FindFirst(IdentityClaims.IdpSub).GetDestinations().ShouldContainOnly("access_token");
    [Fact] void should_carry_the_github_login() => _identity.GetClaim(IdentityClaims.GitHubLogin).ShouldEqual("einari");
    [Fact] void should_carry_the_selected_tenant() => _identity.GetClaim(IdentityClaims.TenantId).ShouldEqual("cratis");
    [Fact] void should_carry_the_client_as_authorized_party() => _identity.GetClaim("azp").ShouldEqual(ClientId);
    [Fact] void should_bind_the_audience_of_the_grant() => _identity.GetResources().ShouldContainOnly(Configurations.DirectAudience);
    [Fact] void should_carry_the_granted_scopes() => _identity.GetScopes().ShouldContainOnly("direct:read", "offline_access");
    [Fact] void should_send_the_tenant_to_the_access_token() => _identity.FindFirst(IdentityClaims.TenantId).GetDestinations().ShouldContainOnly("access_token");
    [Fact] void should_send_the_subject_to_the_access_token() => _identity.FindFirst("sub").GetDestinations().ShouldContainOnly("access_token");
    [Fact] void should_not_send_any_claim_to_an_identity_token() => _identity.Claims.Any(_ => _.HasDestination("id_token")).ShouldBeFalse();
}
