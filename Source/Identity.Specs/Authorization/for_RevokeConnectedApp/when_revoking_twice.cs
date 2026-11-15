// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Authorization.for_RevokeConnectedApp;

public class when_revoking_twice : given.a_connected_app
{
    readonly GrantFence _originalFence = new(Guid.NewGuid(), 1, "grant", Subject, RevocationReason.Revoked, DateTimeOffset.UnixEpoch);

    void Establish() => _fences.Commit("grant", Subject, RevocationReason.Revoked, Arg.Any<CancellationToken>()).Returns(_originalFence);

    async Task Because()
    {
        await Revoke();
        _authorizations.HasStatusAsync(_authorization, OpenIddictConstants.Statuses.Revoked, Arg.Any<CancellationToken>()).Returns(true);
        await Revoke();
    }

    [Fact] void should_accept_the_retry() => _error.ShouldBeNull();
    [Fact] void should_confirm_the_original_fence_again() => _fences.Received(2).ConfirmLog(_originalFence, Arg.Any<CancellationToken>());
}
