// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_the_kernel_reports_an_error : given.a_client_registration_store
{
    Exception _error;

    void Establish() => _store.KernelResult = new()
    {
        Errors = ["AppendOutcomeUnknown"],
        ConcurrencyViolations = [new ConcurrencyViolation(Guid.NewGuid().ToString("D"), 0, 1)]
    };

    async Task Because() => _error = await Catch.Exception(async () => await Reconcile(_confidential));

    [Fact] void should_fail_distinctly() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_the_kernel_message() => _error.Message.ShouldContain("AppendOutcomeUnknown");
    [Fact] void should_not_retry_an_uncertain_append() => _store.Attempts.ShouldEqual(1);
    [Fact] void should_not_materialize_uncommitted_configuration() => _materialized.ShouldBeEmpty();
}
