// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;

namespace Cratis.Identity.for_GrantPolicy.when_evaluating;

public class with_a_known_resource_indicator_and_scopes_of_several_resources : given.a_grant_policy
{
    GrantEvaluation _result;
    Grant _grant;

    void Because()
    {
        _result = _policy.Evaluate(new GrantRequest(["direct:read", "studio:read", "offline_access"], [Configurations.DirectMcp]));
        _grant = (_result as Grantable)?.Grant;
    }

    [Fact] void should_be_grantable() => (_result is Grantable).ShouldBeTrue();
    [Fact] void should_bind_the_grant_to_the_mapped_audience() => _grant.Resource.Audience.ShouldEqual(Configurations.DirectAudience);
    [Fact] void should_keep_the_scope_of_the_resource() => _grant.Scopes.ShouldContain("direct:read");
    [Fact] void should_keep_the_protocol_scope() => _grant.Scopes.ShouldContain("offline_access");
    [Fact] void should_drop_the_scope_of_another_resource() => _grant.Scopes.ShouldNotContain("studio:read");
}
