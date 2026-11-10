// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantAdmission;

public class when_recovery_is_only_partially_covered : given.an_approved_device
{
    async Task Because()
    {
        _gate.Close();
        _gate.MarkGrantsRecovered();
        _accepted = await _admission.CanUse(_principal, true, CancellationToken.None);
    }

    [Fact] void should_first_admit_a_current_live_device_grant() => _baseline.ShouldBeTrue();
    [Fact] void should_not_mistake_grant_replay_for_complete_recovery() => _accepted.ShouldBeFalse();
    [Fact] void should_leave_the_gate_closed() => _gate.Ready.ShouldBeFalse();
}
