// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;

namespace Cratis.Identity.for_GrantPolicy.when_evaluating;

public class with_no_scope_of_the_indicated_resource : given.a_grant_policy
{
    GrantEvaluation _result;

    void Because() => _result = _policy.Evaluate(new GrantRequest(["studio:read", "offline_access"], [Configurations.DirectMcp]));

    [Fact] void should_reject_with_invalid_scope() => ((NotGrantable)_result).Error.ShouldEqual("invalid_scope");
}
