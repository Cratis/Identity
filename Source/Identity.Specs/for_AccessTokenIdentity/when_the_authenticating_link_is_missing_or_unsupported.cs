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

[PerFactSpecificationLifecycle]
public class when_the_authenticating_link_is_missing_or_unsupported : given.an_account_and_a_grant
{
    [Fact] void should_not_choose_an_account_link_without_a_stamp() => ShouldHaveNoUpstreamClaims(Create(null));
    [Fact] void should_not_fall_back_to_another_link_for_an_unknown_stamp() => ShouldHaveNoUpstreamClaims(Create(Guid.NewGuid()));

    [Fact]
    void should_not_invent_a_provider_for_an_account_without_links()
    {
        _account = _account with { LinkedUpstreamKeys = [] };
        ShouldHaveNoUpstreamClaims(Create(null));
    }

    [Fact]
    void should_not_accept_an_unsupported_issuer()
    {
        var link = _account.LinkedUpstreamKeys.Single() with { IdpIssuer = new("https://issuer.example") };
        _account = _account with { LinkedUpstreamKeys = [link] };
        ShouldHaveNoUpstreamClaims(Create(link.LinkId.Value));
    }

    [Fact]
    void should_not_accept_an_unsupported_connection_even_with_the_github_issuer()
    {
        var link = _account.LinkedUpstreamKeys.Single() with { ConnectionId = new(Guid.NewGuid()) };
        _account = _account with { LinkedUpstreamKeys = [link] };
        ShouldHaveNoUpstreamClaims(Create(link.LinkId.Value));
    }

    [Fact]
    void should_not_select_arbitrarily_between_duplicate_link_ids()
    {
        var link = _account.LinkedUpstreamKeys.Single();
        _account = _account with { LinkedUpstreamKeys = [link, link with { IdpSub = new("other-subject") }] };
        ShouldHaveNoUpstreamClaims(Create(link.LinkId.Value));
    }

    [Fact] void should_clear_stale_upstream_claims_from_an_unstamped_grant() => ShouldHaveNoUpstreamClaims(RenewWithStamp(null));
    [Fact] void should_clear_stale_upstream_claims_for_an_unreadable_stamp() => ShouldHaveNoUpstreamClaims(RenewWithStamp("not-a-link"));
    [Fact] void should_clear_stale_upstream_claims_for_an_empty_stamp() => ShouldHaveNoUpstreamClaims(RenewWithStamp(Guid.Empty.ToString("D")));
    [Fact] void should_not_fall_back_after_the_authenticating_link_was_removed() => ShouldHaveNoUpstreamClaims(RenewWithStamp(Guid.NewGuid().ToString("D")));

    ClaimsIdentity Create(Guid? link) => AccessTokenIdentity.Create(_account, ClientId, _grant, new SelectableTenant("cratis", "Cratis"), AuthTime, link);

    ClaimsIdentity RenewWithStamp(string? stamp)
    {
        var granted = new ClaimsPrincipal(new ClaimsIdentity());
        granted.SetClaim(AuthenticatingLink.ClaimType, stamp);
        foreach (var type in new[] { IdentityClaims.Idp, IdentityClaims.IdpSub, IdentityClaims.GitHubId, IdentityClaims.GitHubLogin, Claims.PreferredUsername })
        {
            granted.SetClaim(type, "stale");
        }

        return AccessTokenIdentity.Renew(granted, _account, ClientId, true);
    }

    void ShouldHaveNoUpstreamClaims(ClaimsIdentity identity)
    {
        identity.Claims.Select(_ => _.Type).Intersect([IdentityClaims.Idp, IdentityClaims.IdpSub, IdentityClaims.GitHubId, IdentityClaims.GitHubLogin, Claims.PreferredUsername]).ShouldBeEmpty();
        identity.GetClaim(Claims.Subject).ShouldEqual(_account.Id.Value);
        identity.GetClaim(Claims.Name).ShouldEqual(_account.Name.Value);
    }
}
