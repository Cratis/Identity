// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;

namespace Cratis.Identity.for_GrantPolicy.when_evaluating;

public class with_indicators_of_different_resources : given.a_grant_policy
{
    GrantEvaluation _result;

    void Because() => _result = _policy.Evaluate(new GrantRequest(["direct:read", "studio:read"], [Configurations.DirectMcp, Configurations.StudioMcp]));

    [Fact] void should_reject_with_invalid_target() => ((NotGrantable)_result).Error.ShouldEqual("invalid_target");
}
