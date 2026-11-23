// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts;
using Cratis.Identity.Tenants;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_AccessTokenIdentity;

public class when_authenticating_through_a_specific_github_link : given.an_account_and_a_grant
{
    ClaimsIdentity _identity;

    void Because()
    {
        var later = _account.LinkedUpstreamKeys.Single() with { LinkId = new(Guid.NewGuid()), IdpSub = new("999"), Login = new("later-login"), LinkedAt = AuthTime };
        var other = new LinkedUpstreamKey(new(Guid.NewGuid()), new(Guid.NewGuid()), new("https://issuer.example"), new("other-subject"), new("other-login"), DateTimeOffset.UnixEpoch.AddDays(-1));
        _account = _account with { LinkedUpstreamKeys = [other, .. _account.LinkedUpstreamKeys, later] };
        _identity = AccessTokenIdentity.Create(_account, ClientId, _grant, new SelectableTenant("cratis", "Cratis"), AuthTime, later.LinkId.Value);
    }

    [Fact] void should_use_the_authenticating_issuer() => _identity.GetClaim(IdentityClaims.Idp).ShouldEqual("https://github.com");
    [Fact] void should_use_the_authenticating_subject_not_the_earliest_github_subject() => _identity.GetClaim(IdentityClaims.IdpSub).ShouldEqual("999");
    [Fact] void should_use_the_authenticating_login_not_the_display_login() => _identity.GetClaim(Claims.PreferredUsername).ShouldEqual("later-login");
    [Fact] void should_use_the_authenticating_github_membership_key() => _identity.GetClaim(IdentityClaims.GitHubId).ShouldEqual("999");
    [Fact] void should_use_the_authenticating_github_login() => _identity.GetClaim(IdentityClaims.GitHubLogin).ShouldEqual("later-login");
    [Fact] void should_keep_the_stamp_private() => _identity.FindFirst(AuthenticatingLink.ClaimType).GetDestinations().ShouldBeEmpty();
    [Fact] void should_not_allowlist_the_stamp() => AccessTokenIdentity.AccessTokenClaimTypes.ShouldNotContain(AuthenticatingLink.ClaimType);
    [Fact] void should_not_send_any_claim_to_an_identity_token() => _identity.Claims.Any(_ => _.HasDestination(Destinations.IdentityToken)).ShouldBeFalse();
}
