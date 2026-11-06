// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Identity.Accounts.for_AccountStore;

public class when_the_kernel_reports_concurrency_violations : given.a_rejected_account_append
{
    Exception _error;

    void Establish() => _store.Result = new() { ConcurrencyViolations = [new ConcurrencyViolation(_account.Value, 0, 1)] };

    async Task Because() => _error = await Catch.Exception(TryAppend);

    [Fact] void should_preserve_the_account_refusal() => _error.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_preserve_the_concurrency_reason() => _error.Message.ShouldEqual("account_concurrency_conflict");
    [Fact] void should_not_log_an_infrastructure_failure() => _log.Errors.ShouldBeEmpty();
}
