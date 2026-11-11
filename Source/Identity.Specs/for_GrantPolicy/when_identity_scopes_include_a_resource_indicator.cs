// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;

namespace Cratis.Identity.for_GrantPolicy;

public class when_identity_scopes_include_a_resource_indicator : given.an_oidc_grant_policy
{
    NotGrantable _rejection;

    void Because() => _rejection = (NotGrantable)_policy.Evaluate(new GrantRequest(["openid", "profile"], [Configurations.DirectMcp], "oidc-web"));

    [Fact] void should_not_create_a_resource_grant() => _rejection.Error.ShouldEqual("invalid_target");
}
