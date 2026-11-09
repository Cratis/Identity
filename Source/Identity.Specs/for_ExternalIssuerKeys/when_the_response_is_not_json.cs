// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_ExternalIssuerKeys;

public class when_the_response_is_not_json : given.an_issuer_that_serves_keys
{
    void Establish() => _respond = () => Json(_fixture.JwksDocument, "text/html");

    async Task Because() => await Get();

    [Fact] void should_return_no_keys() => _result.ShouldBeNull();
}
