// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_GrantPolicy;

public class when_profile_is_requested_without_openid : given.an_oidc_grant_policy
{
    NotGrantable _rejection;

    void Because() => _rejection = (NotGrantable)_policy.Evaluate(new GrantRequest(["profile"], [], "oidc-web"));

    [Fact] void should_require_openid() => _rejection.Error.ShouldEqual("invalid_scope");
}
