// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_GrantPolicy;

public class when_identity_and_resource_scopes_are_requested : given.an_oidc_grant_policy
{
    Grantable _result;

    void Because() => _result = (Grantable)_policy.Evaluate(new GrantRequest(["openid", "direct:read"], [], "oidc-web"));

    [Fact] void should_keep_the_resource_binding() => _result.Grant.Resource.Owns("direct:read").ShouldBeTrue();
}
