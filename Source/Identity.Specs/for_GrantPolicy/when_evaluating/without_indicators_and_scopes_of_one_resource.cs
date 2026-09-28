// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;

namespace Cratis.Identity.for_GrantPolicy.when_evaluating;

public class without_indicators_and_scopes_of_one_resource : given.a_grant_policy
{
    GrantEvaluation _result;

    void Because() => _result = _policy.Evaluate(new GrantRequest(["direct:read", "direct:work", "offline_access"], []));

    [Fact] void should_infer_the_resource_from_the_scopes() => ((Grantable)_result).Grant.Resource.Audience.ShouldEqual(Configurations.DirectAudience);
    [Fact] void should_grant_the_requested_scopes() => ((Grantable)_result).Grant.Scopes.ShouldContainOnly("direct:read", "direct:work", "offline_access");
}
