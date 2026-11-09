// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts;
using Cratis.Identity.given;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_AccessTokenIdentity;

public class when_exchanging : given.an_account_and_a_grant
{
    ClaimsIdentity _identity;

    void Because() => _identity = AccessTokenIdentity.Exchange(
        _account,
        "authproxy",
        _grant.Resource,
        "cratis",
        ["direct:read"],
        Configurations.ExternalIssuer,
        "external-subject-1",
        AuthTime.ToUnixTimeSeconds(),
        TimeSpan.FromMinutes(5));

    [Fact] void should_not_invent_a_grant_id_for_an_exchange() => _identity.GetClaim("cid").ShouldBeNull();
    [Fact] void should_use_the_account_id_as_subject() => _identity.GetClaim(Claims.Subject).ShouldEqual(_account.Id.Value);
    [Fact] void should_carry_the_account_name() => _identity.GetClaim(Claims.Name).ShouldEqual(_account.Name.Value);
    [Fact] void should_carry_the_client_as_authorized_party() => _identity.GetClaim(Claims.AuthorizedParty).ShouldEqual("authproxy");
    [Fact] void should_carry_the_requested_tenant() => _identity.GetClaim(IdentityClaims.TenantId).ShouldEqual("cratis");
    [Fact] void should_bind_the_audience_of_the_resource() => _identity.GetResources().ShouldContainOnly(Configurations.DirectAudience);
    [Fact] void should_carry_only_the_requested_scopes() => _identity.GetScopes().ShouldContainOnly("direct:read");
    [Fact] void should_not_carry_offline_access() => _identity.GetScopes().ShouldNotContain("offline_access");
    [Fact] void should_name_the_external_issuer_as_idp() => _identity.GetClaim(IdentityClaims.Idp).ShouldEqual(Configurations.ExternalIssuer);
    [Fact] void should_carry_the_external_subject_for_an_upstream_identity_consumer() => _identity.GetClaim(IdentityClaims.IdpSub).ShouldEqual("external-subject-1");
    [Fact] void should_not_carry_a_github_login_next_to_another_issuers_identity() => _identity.GetClaim(Claims.PreferredUsername).ShouldBeNull();
    [Fact] void should_not_carry_github_claims() => _identity.Claims.Select(_ => _.Type).Intersect([IdentityClaims.GitHubId, IdentityClaims.GitHubLogin]).ShouldBeEmpty();
    [Fact] void should_carry_basic_assurance() => _identity.GetClaim(Claims.AuthenticationContextReference).ShouldEqual("basic");
    [Fact] void should_copy_the_authentication_time() => _identity.GetClaim(Claims.AuthenticationTime).ShouldEqual(AuthTime.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
    [Fact] void should_send_every_allowlisted_claim_only_to_the_access_token() => _identity.Claims.Where(_ => AccessTokenIdentity.AccessTokenClaimTypes.Contains(_.Type)).All(_ => _.GetDestinations().SequenceEqual([Destinations.AccessToken])).ShouldBeTrue();
    [Fact] void should_not_send_anything_to_an_identity_token_or_refresh_token() => _identity.Claims.Any(_ => _.HasDestination(Destinations.IdentityToken)).ShouldBeFalse();
    [Fact] void should_not_carry_delegation_or_binding_claims() => _identity.Claims.Select(_ => _.Type).Intersect(["act", "may_act", "cnf", "org"]).ShouldBeEmpty();
    [Fact] void should_set_the_access_token_lifetime() => new ClaimsPrincipal(_identity).GetAccessTokenLifetime().ShouldEqual(TimeSpan.FromMinutes(5));
}
