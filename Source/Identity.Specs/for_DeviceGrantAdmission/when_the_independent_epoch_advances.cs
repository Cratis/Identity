// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantAdmission;

public class when_the_independent_epoch_advances : given.an_approved_device
{
    async Task Because()
    {
        _log.RecoveryEpoch(Arg.Any<CancellationToken>()).Returns(8L);
        _accepted = await _admission.CanUse(_principal, true, CancellationToken.None);
    }

    [Fact] void should_have_admitted_the_same_grant_before_restore() => _baseline.ShouldBeTrue();
    [Fact] void should_refuse_the_old_capability() => _accepted.ShouldBeFalse();
    [Fact] void should_close_the_gate() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_observe_the_new_epoch() => _gate.Epoch.ShouldEqual(8L);
}
