// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Identity.Accounts.Linking.for_AccountLinking;

public class when_both_append_attempts_conflict : given.an_account_linking_adapter
{
    Exception _exception;

    void Establish()
    {
        var conflict = new CommandResult { ValidationResults = [ValidationResult.Error("Conflict") with { Reason = ValidationResultReason.ConcurrencyViolation }] };
        _results.Enqueue(conflict);
        _results.Enqueue(conflict);
    }

    async Task Because() => _exception = await Catch.Exception(Propose);

    [Fact] void should_retry_only_once() => _order.ShouldEqual(["execute", "wait", "execute"]);
    [Fact] void should_refuse_after_the_retry() => _exception.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_preserve_the_concurrency_reason() => _exception.Message.ShouldEqual("account_concurrency_conflict");
}
