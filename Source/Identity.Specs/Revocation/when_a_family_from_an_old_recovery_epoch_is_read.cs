// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_family_from_an_old_recovery_epoch_is_read : Specification
{
    ReadRefreshGrant _reader;
    GrantState? _result;

    void Establish()
    {
        var gate = new RecoveryGate();
        gate.ObserveEpoch(1);
        gate.Open(true);
        var fences = Substitute.For<IFenceStore>();
        var entry = OutboxEntry.Create(FactKind.FamilyStarted, "grant", Guid.NewGuid().ToString("D"), DateTimeOffset.UnixEpoch, DateTimeOffset.UtcNow.AddDays(1));
        fences.Read("grant", Arg.Any<CancellationToken>()).Returns(new GrantState("grant", 1, "token", null, [entry], 0));
        _reader = new(fences, Substitute.For<IOpenIddictTokenManager>(), Substitute.For<IOpenIddictAuthorizationManager>(), TimeProvider.System, gate);
    }

    async Task Because() => _result = await _reader.Read("grant", "token", new object(), new ClaimsPrincipal(), CancellationToken.None);

    [Fact] void should_not_treat_a_valid_looking_old_family_as_current_permission() => _result.ShouldBeNull();
}
