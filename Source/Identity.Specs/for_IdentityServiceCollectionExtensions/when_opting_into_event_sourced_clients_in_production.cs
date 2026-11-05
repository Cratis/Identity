// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_opting_into_event_sourced_clients_in_production : Specification
{
    IServiceCollection _services;

    void Because() => _services = new ServiceCollection().AddEventSourcedClients();

    [Fact] void should_not_have_a_development_only_guard() => _services.Any(service => service.ImplementationType == typeof(EventSourcedClientRegistrations)).ShouldBeTrue();
}
