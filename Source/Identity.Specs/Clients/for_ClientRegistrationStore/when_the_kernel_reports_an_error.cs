// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_ClientRegistrationStore;

public class when_the_kernel_reports_an_error : given.a_client_registration_store
{
    const string Payload = "secret-payload-must-not-escape";
    ClientRegistrationId _id;
    Exception _error;

    void Establish()
    {
        _id = ClientRegistrationId.New();
        _store.KernelResult = new() { Errors = ["ConstraintIndexUnavailable", $"Could not serialize {{clientSecret: '{Payload}'}}"] };
    }

    async Task Because()
    {
        var read = await _store.Read(_id);
        _error = await Catch.Exception(async () => await _store.Append(_id, [new ClientRegistered(ClientId.Create("example"), RegistrationKind.PreRegistered, DateTimeOffset.UtcNow)], read));
    }

    [Fact] void should_report_an_infrastructure_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_report_a_concurrency_failure() => _error.Message.ShouldContain("Chronicle append failed;");
    [Fact] void should_include_the_safe_kernel_message() => _error.Message.ShouldContain("ConstraintIndexUnavailable");
    [Fact] void should_include_the_source() => _error.Message.ShouldContain(_id.Value.ToString("D"));
    [Fact] void should_include_the_event_type_id() => _error.Message.ShouldContain("420e2bb2-039f-443a-9491-1a6323788add");
    [Fact] void should_indicate_missing_sequence_metadata() => _error.Message.ShouldContain("expected sequence: not reported, actual sequence: not reported");
    [Fact] void should_redact_arbitrary_kernel_text() => _error.Message.ShouldContain("[redacted kernel error]");
    [Fact] void should_not_expose_payloads() => _error.ToString().ShouldNotContain(Payload);
    [Fact] void should_log_the_same_safe_fields() => _appendLog.Errors.ShouldContain(_error.Message);
    [Fact] void should_not_log_payloads() => string.Join("; ", _appendLog.Errors).ShouldNotContain(Payload);
}
