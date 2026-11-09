// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_recovery_is_unproven : given.a_validated_access_token
{
    void Establish() => _gate.Close();
    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_fail_closed() => _context.IsRejected.ShouldBeTrue();
    [Fact] void should_not_leak_ids_or_the_cache_epoch() => _context.Claims.ShouldBeEmpty();
}
