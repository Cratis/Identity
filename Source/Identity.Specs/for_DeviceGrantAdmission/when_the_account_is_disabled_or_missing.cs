// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.for_DeviceGrantAdmission;

public class when_the_account_is_disabled_or_missing : given.an_approved_device
{
    async Task Because()
    {
        _accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns((Account?)null);
        _accepted = await _admission.CanUse(_principal, true, CancellationToken.None);
    }

    [Fact] void should_admit_while_the_active_account_exists() => _baseline.ShouldBeTrue();
    [Fact] void should_not_redeem_after_the_account_loses_admission() => _accepted.ShouldBeFalse();
}
