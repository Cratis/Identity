// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.RevocationSpecs;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_DeviceGrantAdmission;

public class when_a_primary_grant_fence_exists : given.an_approved_device
{
    bool _known;
    async Task Because()
    {
        var authority = new SharedFenceStore();
        await authority.Commit("authorization", _principal.GetClaim(OpenIddictConstants.Claims.Subject), RevocationReason.Revoked, CancellationToken.None);
        var state = (await authority.Read("authorization", CancellationToken.None)) with { RecoveryEpoch = 7 };
        _known = GrantStateValidation.IsKnown(state);
        _fences.Read("authorization", Arg.Any<CancellationToken>()).Returns(state);
        _accepted = await _admission.CanUse(_principal, true, CancellationToken.None);
    }

    [Fact] void should_first_admit_the_unfenced_grant() => _baseline.ShouldBeTrue();
    [Fact] void should_test_a_well_formed_primary_fence() => _known.ShouldBeTrue();
    [Fact] void should_deny_without_waiting_for_a_read_model() => _accepted.ShouldBeFalse();
}
