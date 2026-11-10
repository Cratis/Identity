// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_the_validated_principal_is_missing : given.a_validated_access_token
{
    void Establish() => _context.GenericTokenPrincipal = null!;
    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_not_add_credential_metadata() => _context.Claims.ShouldBeEmpty();
}
