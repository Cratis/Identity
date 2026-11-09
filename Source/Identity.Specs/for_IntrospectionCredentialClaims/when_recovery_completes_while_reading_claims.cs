// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_recovery_completes_while_reading_claims : given.a_validated_access_token
{
    void Establish() => _context.GenericTokenPrincipal = new RecoveringPrincipal(_context.GenericTokenPrincipal, () =>
    {
        _gate.ObserveEpoch(8);
        _gate.Open(true);
    });

    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_have_completed_recovery_during_the_read() => _gate.Epoch.ShouldEqual(8L);
    [Fact] void should_fail_closed_even_though_recovery_is_ready_again() => _context.Error.ShouldEqual(Errors.InvalidToken);
    [Fact] void should_discard_all_claims() => _context.Claims.ShouldBeEmpty();
    [Fact] void should_discard_all_scopes() => _context.Scopes.ShouldBeEmpty();

    sealed class RecoveringPrincipal(ClaimsPrincipal principal, Action recover) : ClaimsPrincipal(principal)
    {
        public override IEnumerable<Claim> FindAll(string type)
        {
            if (type == OpenIddictConstants.Claims.Private.AuthorizationId) recover();
            return base.FindAll(type);
        }
    }
}
