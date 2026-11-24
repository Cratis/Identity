// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Contracts.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.Clients.Serving.for_RegisteredClients;

public class when_serving_registrations : given.client_commands
{
    ClientRegistration _retired;
    ClientRegistration _reserved;
    ClientRegistration _invalid;
    ClientRegistration _unannounced;
    IReadOnlyList<Protocol.Stores.ClientApplication> _served;

    void Establish()
    {
        _retired = _active with { Id = Guid.NewGuid(), ClientId = new("retired-client"), Retired = true };
        _reserved = _active with { Id = Guid.NewGuid(), ClientId = new("cratis-cli") };
        _invalid = _active with { Id = Guid.NewGuid(), ClientId = new("broken-client"), Configuration = _configuration with { ClientType = ClientType.Public, RequirePkce = false } };
        _unannounced = new(Guid.NewGuid());
    }

    void Because() => _served = RegisteredClients.Serve(
        [_active, _retired, _reserved, _invalid, _unannounced],
        new HashSet<string?>(StringComparer.Ordinal) { "cratis-cli" },
        NullLogger.Instance);

    [Fact] void should_serve_the_active_client() => _served.Select(application => application.ClientId).ShouldContainOnly("spec-client");
    [Fact] void should_name_the_application_by_the_registration() => _served.Single().Id.Length.ShouldEqual(24);
    [Fact] void should_keep_the_registration_for_verifying_secrets() => _served.Single().Registration.ShouldEqual(_active);
}
