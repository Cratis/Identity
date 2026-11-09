// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_the_validation_epoch_is_missing : given.a_validated_access_token
{
    void Establish() => _context.Transaction.Properties.Remove(IntrospectionCredentialClaims.RecoveryEpochProperty);
    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_fail_closed() => _context.Error.ShouldEqual(Errors.InvalidToken);
    [Fact] void should_not_publish_claims() => _context.Claims.ShouldBeEmpty();
}
