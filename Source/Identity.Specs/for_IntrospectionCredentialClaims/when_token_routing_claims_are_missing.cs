// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_token_routing_claims_are_missing : given.a_validated_access_token
{
    void Establish()
    {
        _context.GenericTokenPrincipal.SetClaim("tid", (string?)null);
        _context.GenericTokenPrincipal.SetAudiences([]);
    }

    async Task Because() => await Populate();

    [Fact] void should_not_take_a_tenant_from_the_request() => _context.Claims.ContainsKey("tid").ShouldBeFalse();
    [Fact] void should_not_take_an_audience_from_the_request() => _context.Audiences.ShouldBeEmpty();
}
