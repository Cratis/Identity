// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        await _handler.HandleAsync(_context);
    }

    [Fact] void should_report_the_new_independent_epoch() => ((long?)_context.Claims["cache_epoch"]).ShouldEqual(8L);
    [Fact] void should_change_the_cache_boundary() => (((long?)_context.Claims["cache_epoch"]) != _previous).ShouldBeTrue();
}
