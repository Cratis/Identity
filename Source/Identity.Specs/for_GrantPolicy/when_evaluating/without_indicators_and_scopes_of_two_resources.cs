// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_GrantPolicy.when_evaluating;

public class without_indicators_and_scopes_of_two_resources : given.a_grant_policy
{
    GrantEvaluation _result;

    void Because() => _result = _policy.Evaluate(new GrantRequest(["direct:read", "studio:read"], []));

    [Fact] void should_reject_with_invalid_target() => ((NotGrantable)_result).Error.ShouldEqual("invalid_target");
}
