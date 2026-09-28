// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;

namespace Cratis.Identity.for_GrantPolicy.when_evaluating;

public class with_two_indicators_of_the_same_resource : given.a_grant_policy
{
    GrantEvaluation _result;

    void Because() => _result = _policy.Evaluate(new GrantRequest(["direct:read"], [Configurations.DirectMcp, Configurations.DirectV1]));

    [Fact] void should_bind_the_grant_to_the_single_audience() => ((Grantable)_result).Grant.Resource.Audience.ShouldEqual(Configurations.DirectAudience);
}
