// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_introspection_was_rejected : given.a_validated_access_token
{
    void Establish() => _context.Reject("invalid_token");
    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_not_add_any_members() => _context.Claims.ShouldBeEmpty();
}
