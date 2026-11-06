// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_ClientRegistrationStore;

public class when_the_kernel_reports_constraint_violations : given.a_client_registration_store
{
    ClientRegistrationOutcome _outcome;

    void Establish() => _store.KernelResult = new()
    {
        ConstraintViolations = [new("420e2bb2-039f-443a-9491-1a6323788add", 0, ConstraintType.Unique, "client-id", "Already reserved", new())]
    };

    async Task Because()
    {
        var id = ClientRegistrationId.New();
        var read = await _store.Read(id);
        _outcome = await _store.Append(id, [new ClientRegistered(ClientId.Create("example"), RegistrationKind.PreRegistered, DateTimeOffset.UtcNow)], read);
    }

    [Fact] void should_preserve_the_registration_conflict() => _outcome.ShouldEqual(ClientRegistrationOutcome.AlreadyRegistered);
    [Fact] void should_not_log_an_infrastructure_failure() => _appendLog.Errors.ShouldBeEmpty();
}
