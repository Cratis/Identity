// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;

namespace Cratis.Identity.for_GrantPolicy.when_evaluating_a_token_request;

public class for_an_unknown_resource : given.a_grant_policy
{
    NotGrantable? _result;

    void Because() => _result = _policy.EvaluateTokenRequest(["https://evil.example/"], [Configurations.DirectAudience]);

    [Fact] void should_reject_with_invalid_target() => _result.Error.ShouldEqual("invalid_target");
}
