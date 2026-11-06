// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Identity.Accounts.for_AccountStore;

public class when_the_kernel_reports_constraint_violations : given.a_rejected_account_append
{
    bool _committed;

    void Establish() => _store.Result = new()
    {
        ConstraintViolations = [new("92fde774-1586-4d45-a354-8349a9f5e3fd", 0, ConstraintType.Unique, "upstream-key", "Already reserved", new())]
    };

    async Task Because() => _committed = await TryAppend();

    [Fact] void should_preserve_the_uniqueness_result() => _committed.ShouldBeFalse();
    [Fact] void should_not_log_an_infrastructure_failure() => _log.Errors.ShouldBeEmpty();
}
