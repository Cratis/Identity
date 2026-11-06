// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Identity.Accounts.for_AccountStore;

public class when_the_kernel_reports_an_error : given.a_rejected_account_append
{
    const string Payload = "private@example.test";
    Exception _error;

    void Establish() => _store.Result = new()
    {
        Errors = ["ConstraintIndexUnavailable", $"Invalid content: {Payload}"],
        ConcurrencyViolations = [new ConcurrencyViolation(Payload, 12, 13) { ConflictingEventTypeId = Payload }]
    };

    async Task Because() => _error = await Catch.Exception(TryAppend);

    [Fact] void should_not_mask_errors_as_account_conflicts() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_the_safe_kernel_message() => _error.Message.ShouldContain("ConstraintIndexUnavailable");
    [Fact] void should_include_the_source() => _error.Message.ShouldContain(_account.Value);
    [Fact] void should_include_the_event_type_id() => _error.Message.ShouldContain("92fde774-1586-4d45-a354-8349a9f5e3fd");
    [Fact] void should_include_the_sequence_comparison() => _error.Message.ShouldContain("expected sequence: 12, actual sequence: 13");
    [Fact] void should_not_expose_event_content() => _error.ToString().ShouldNotContain(Payload);
    [Fact] void should_indicate_redacted_kernel_text() => _error.Message.ShouldContain("[redacted kernel error]");
    [Fact] void should_redact_untrusted_identifiers() => _error.Message.ShouldContain("event source id: [redacted identifier], event type ids: [redacted identifier]");
    [Fact] void should_log_the_same_safe_fields() => _log.Errors.ShouldContain(_error.Message);
    [Fact] void should_not_log_event_content() => string.Join("; ", _log.Errors).ShouldNotContain(Payload);
}
