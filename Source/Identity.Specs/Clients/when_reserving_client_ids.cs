// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients;

public class when_reserving_client_ids : given.client_commands
{
    [Fact]
    async Task should_refuse_duplicate_ids_even_after_retirement()
    {
        using var scenario = new EventScenario();
        await scenario.Given.ForEventSource(_id.Value).Events(_configuration.Registered(new("reserved-client"), Now));
        var duplicate = await scenario.When.ForEventSource(ClientRegistrationId.New().Value).Events(_configuration.Registered(new("reserved-client"), Now));
        duplicate.Errors.ShouldBeEmpty();
        duplicate.HasConstraintViolations.ShouldBeTrue();
        await scenario.Given.ForEventSource(_id.Value).Events(new ClientRetired("owner_retired", Now));
        var retired = await scenario.When.ForEventSource(ClientRegistrationId.New().Value).Events(_configuration.Registered(new("reserved-client"), Now));
        retired.Errors.ShouldBeEmpty();
        retired.HasConstraintViolations.ShouldBeTrue();
        var distinct = await scenario.When.ForEventSource(ClientRegistrationId.New().Value).Events(_configuration.Registered(new("distinct-client"), Now));
        distinct.Errors.ShouldBeEmpty();
        distinct.IsSuccess.ShouldBeTrue();
    }
}
