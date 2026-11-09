// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.for_DeviceGrantAdmission;

public class when_the_authorization_is_no_longer_valid : given.an_approved_device
{
    async Task Because()
    {
        _authorizations.HasStatusAsync(_authorization, OpenIddictConstants.Statuses.Valid, Arg.Any<CancellationToken>()).Returns(false);
        _accepted = await _admission.CanUse(_principal, true, CancellationToken.None);
    }

    [Fact] void should_admit_the_live_authorization() => _baseline.ShouldBeTrue();
    [Fact] void should_reject_the_revoked_authorization() => _accepted.ShouldBeFalse();
}
