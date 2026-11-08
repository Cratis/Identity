// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_AccessTokenIdentity;

public class when_creating_for_a_resource_that_does_not_receive_upstream_identity : given.an_account_and_a_grant
{
    ClaimsIdentity _identity;

    void Because()
    {
        var studio = new ResourceCatalog(Configurations.TwoResourceServers().AsOptions()).FindByAudience(Configurations.StudioAudience);
        _identity = AccessTokenIdentity.Create(_account, ClientId, new Grant(studio, ["studio:read"]), new SelectableTenant("studio-tenant", "Studio"), AuthTime);
    }

    [Fact] void should_not_flag_the_resource_by_default() => Configurations.TwoResourceServers().Resources[1].ReceivesUpstreamIdentity.ShouldBeFalse();
    [Fact] void should_not_carry_the_idp_sub() => _identity.FindFirst(IdentityClaims.IdpSub).ShouldBeNull();
    [Fact] void should_not_carry_the_github_id() => _identity.FindFirst(IdentityClaims.GitHubId).ShouldBeNull();
    [Fact] void should_not_carry_the_github_login() => _identity.FindFirst(IdentityClaims.GitHubLogin).ShouldBeNull();
    [Fact] void should_not_carry_the_preferred_username() => _identity.FindFirst(Claims.PreferredUsername).ShouldBeNull();
    [Fact] void should_still_carry_the_display_name_as_name() => _identity.GetClaim(Claims.Name).ShouldEqual("Einar Ingebrigtsen");
    [Fact] void should_still_carry_the_subject() => _identity.GetClaim(Claims.Subject).ShouldEqual(_account.Id.Value);
    [Fact] void should_still_carry_the_idp() => _identity.GetClaim(IdentityClaims.Idp).ShouldEqual("https://github.com");
    [Fact] void should_still_carry_the_authentication_time() => _identity.GetClaim("auth_time").ShouldEqual("1700000000");
}
