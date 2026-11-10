// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantAdmission;

public class when_the_code_epoch_is_not_current : given.an_approved_device
{
    bool _missing;
    async Task Because()
    {
        _tokens.GetPropertiesAsync(_token, Arg.Any<CancellationToken>()).Returns(Stamp(6));
        _accepted = await _admission.CanUse(_principal, true, CancellationToken.None);
        _tokens.GetPropertiesAsync(_token, Arg.Any<CancellationToken>()).Returns([]);
        _missing = await _admission.CanUse(_principal, true, CancellationToken.None);
    }

    [Fact] void should_admit_the_original_current_code() => _baseline.ShouldBeTrue();
    [Fact] void should_reject_a_stale_code() => _accepted.ShouldBeFalse();
    [Fact] void should_reject_an_unstamped_code() => _missing.ShouldBeFalse();
}
