// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_GrantPolicy;

public class when_only_identity_scopes_are_requested : given.an_oidc_grant_policy
{
    IdentityOnlyGrant _grant;

    void Because() => _grant = (IdentityOnlyGrant)_policy.Evaluate(new GrantRequest(["openid", "profile"], [], "oidc-web"));

    [Fact] void should_grant_only_the_requested_identity_scopes() => _grant.Scopes.ShouldContainOnly("openid", "profile");
}
