// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_AccessTokenIdentity;

public class when_exchanging_for_a_resource_that_does_not_receive_upstream_identity : given.an_account_and_a_grant
{
    ClaimsIdentity _identity;

    void Because()
    {
        var studio = new ResourceCatalog(Configurations.TwoResourceServers().AsOptions()).FindByAudience(Configurations.StudioAudience);
        _identity = AccessTokenIdentity.Exchange(_account, "authproxy", studio, "studio-tenant", ["studio:read"], Configurations.ExternalIssuer, "external-subject-1", null, TimeSpan.FromMinutes(5));
    }

    [Fact] void should_not_carry_the_external_subject() => _identity.GetClaim(IdentityClaims.IdpSub).ShouldBeNull();
    [Fact] void should_not_carry_the_preferred_username() => _identity.GetClaim(Claims.PreferredUsername).ShouldBeNull();
    [Fact] void should_still_name_the_issuer() => _identity.GetClaim(IdentityClaims.Idp).ShouldEqual(Configurations.ExternalIssuer);
    [Fact] void should_not_invent_an_authentication_time() => _identity.GetClaim(Claims.AuthenticationTime).ShouldBeNull();
    [Fact] void should_bind_the_studio_audience() => _identity.GetResources().ShouldContainOnly(Configurations.StudioAudience);
}
