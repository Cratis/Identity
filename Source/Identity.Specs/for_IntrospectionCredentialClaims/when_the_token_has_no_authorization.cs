// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_the_token_has_no_authorization : given.a_validated_access_token
{
    void Establish() => _context.GenericTokenPrincipal.SetAuthorizationId(null);
    async Task Because() => await Populate();

    [Fact] void should_not_invent_a_credential_id() => _context.Claims.ContainsKey("credential_id").ShouldBeFalse();
    [Fact] void should_not_identify_an_exchange_as_a_grant() => _context.Claims.ContainsKey("credential_kind").ShouldBeFalse();
}
