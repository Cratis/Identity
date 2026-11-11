// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_GrantPolicy;

public class when_an_unregistered_client_requests_openid : given.an_oidc_grant_policy
{
    NotGrantable _rejection;

    void Because() => _rejection = (NotGrantable)_policy.Evaluate(new GrantRequest(["openid", "profile"], [], "cratis-cli"));

    [Fact] void should_refuse_the_identity_scopes() => _rejection.Error.ShouldEqual("invalid_scope");
}
