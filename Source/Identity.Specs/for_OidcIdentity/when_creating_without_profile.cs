// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_OidcIdentity;

public class when_creating_without_profile : given.an_identity_grant
{
    ClaimsIdentity _identity;

    void Because() => _identity = OidcIdentity.Create(new(_account, AuthTime), ClientId, new IdentityOnlyGrant(["openid"]));

    [Fact] void should_not_add_name() => _identity.GetClaim(Claims.Name).ShouldBeNull();
    [Fact] void should_not_add_preferred_username() => _identity.GetClaim(Claims.PreferredUsername).ShouldBeNull();
    [Fact] void should_not_invent_a_session_id() => _identity.GetClaim(OidcIdentity.SessionIdClaim).ShouldBeNull();
    [Fact] void should_send_the_subject_to_both_tokens() => _identity.FindFirst(Claims.Subject).GetDestinations().ShouldContainOnly(Destinations.AccessToken, Destinations.IdentityToken);
}
