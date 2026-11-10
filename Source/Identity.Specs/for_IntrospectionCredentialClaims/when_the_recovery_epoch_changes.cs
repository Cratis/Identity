// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_the_recovery_epoch_changes : given.a_validated_access_token
{
    long? _previous;

    async Task Because()
    {
        await Populate();
        _previous = (long?)_context.Claims["cache_epoch"];
        _gate.ObserveEpoch(8);
        _gate.Open(true);
        await _validation.HandleAsync(new OpenIddictServerEvents.ValidateTokenContext(_context.Transaction));
        await _handler.HandleAsync(_context);
    }

    [Fact] void should_have_reported_the_validated_epoch_before_recovery() => _previous.ShouldEqual(7L);
    [Fact] void should_preserve_the_first_validation_epoch() => _context.Transaction.Properties[IntrospectionCredentialClaims.RecoveryEpochProperty].ShouldEqual(7L);
    [Fact] void should_fail_closed_after_recovery_completes() => _context.Error.ShouldEqual(Errors.InvalidToken);
    [Fact] void should_not_publish_the_new_epoch_with_old_claims() => _context.Claims.ShouldBeEmpty();
    [Fact] void should_not_leave_old_scopes() => _context.Scopes.ShouldBeEmpty();
}
