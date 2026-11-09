// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_the_caller_is_not_in_the_token_audience : given.a_validated_access_token
{
    async Task Because()
    {
        _context.Request.ClientId = "some-other-client";
        await Populate();
    }

    [Fact] void should_not_attach_the_credential_id() => _context.Claims.ContainsKey("credential_id").ShouldBeFalse();
    [Fact] void should_not_attach_the_tenant() => _context.Claims.ContainsKey("tid").ShouldBeFalse();
    [Fact] void should_not_attach_the_cache_epoch() => _context.Claims.ContainsKey("cache_epoch").ShouldBeFalse();
}
