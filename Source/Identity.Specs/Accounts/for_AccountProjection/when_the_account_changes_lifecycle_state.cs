// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountProjection;

public class when_the_account_changes_lifecycle_state : Specification
{
    [Theory]
    [InlineData(AccountStatus.Disabled)]
    [InlineData(AccountStatus.Active)]
    [InlineData(AccountStatus.Tombstoned)]
    public async Task should_project_the_current_lifecycle_state(AccountStatus expected)
    {
        using var scenario = new ReadModelScenario<Account>();
        var subject = AccountSubject.New();
        object last = expected switch
        {
            AccountStatus.Disabled => new AccountDisablementRecorded("spec_reason", 1UL, new(Guid.NewGuid()), new(1), FactId.New()),
            AccountStatus.Active => new AccountEnablementRecorded(2UL, DateTimeOffset.UnixEpoch, FactId.New()),
            _ => new AccountTombstoneRecorded(DateTimeOffset.UnixEpoch, FactId.New())
        };
        await scenario.Given.ForEventSource(subject.Value).Events(new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()), last);
        scenario.Instance.Status.ShouldEqual(expected);
    }
}
