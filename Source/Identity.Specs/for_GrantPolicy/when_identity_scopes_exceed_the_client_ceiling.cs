// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_GrantPolicy;

public class when_identity_scopes_exceed_the_client_ceiling : given.an_oidc_grant_policy
{
    NotGrantable _rejection;

    void Because() => _rejection = (NotGrantable)_policy.Evaluate(new GrantRequest(["openid", "profile", "offline_access"], [], "oidc-openid-only"));

    [Fact] void should_refuse_scopes_outside_the_registered_ceiling() => _rejection.Error.ShouldEqual("invalid_scope");
}
