// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_the_kernel_reports_concurrency_violations : given.a_client_registration_store
{
    Exception _error;

    void Establish() => _store.KernelResult = new()
    {
        ConcurrencyViolations = [new ConcurrencyViolation(Guid.NewGuid().ToString("D"), 0, 1)],
        ConcurrencyCheckPerformed = true
    };

    async Task Because() => _error = await Catch.Exception(async () => await Reconcile(_confidential));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_report_a_real_conflict() => _error.Message.ShouldEqual("Client registration changed concurrently; reconciliation failed closed.");
    [Fact] void should_retry_only_once() => _store.Attempts.ShouldEqual(2);
    [Fact] void should_not_materialize_uncommitted_configuration() => _materialized.ShouldBeEmpty();
    [Fact] void should_not_log_an_infrastructure_failure() => _appendLog.Errors.ShouldBeEmpty();
}
