// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registering;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Clients;

public class when_admitting_client_ids : given.client_commands
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    [InlineData("contains:colon")]
    [InlineData("tab\tinside")]
    [InlineData("line\nbreak")]
    [InlineData("non-ascii-\u00e9")]
    [InlineData("delete-\u007f")]
    void should_refuse_ids_outside_the_exact_vschar_policy(string? value)
    {
        var command = new RegisterClient(value is null ? null : new(value), _configuration, Plaintext);
        command.Handle(_descriptors, _clock, _digester).TryGetError(out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(128, false)]
    [InlineData(129, true)]
    void should_enforce_id_length_without_truncation(int length, bool refused)
    {
        var command = new RegisterClient(new(new string('x', length)), _configuration, Plaintext);
        command.Handle(_descriptors, _clock, _digester).TryGetError(out _).ShouldEqual(refused);
    }

    [Fact]
    void should_preserve_case_and_internal_vschar_spaces()
    {
        var command = new RegisterClient(new("Case Sensitive Id!"), _configuration, Plaintext);
        var registered = command.Handle(_descriptors, _clock, _digester).AsT0.First().Event;
        ((Contracts.Clients.Events.ClientRegistered)registered).ClientId.ShouldEqual("Case Sensitive Id!");
    }

    [Theory]
    [InlineData(ClientDescriptors.CratisCli, true)]
    [InlineData("configured-client", true)]
    [InlineData("Configured-client", false)]
    void should_read_exact_configured_id_reservations_before_registering(string id, bool reserved)
    {
        var options = Options.Create(new IdentityOptions
        {
            IntrospectionClients = [new() { ClientId = "configured-client", ClientSecret = Plaintext }]
        });
        var descriptors = new ClientDescriptors(new ResourceCatalog(options), options);
        var command = new RegisterClient(new(id), _configuration, Plaintext);
        var result = command.Handle(descriptors, _clock, _digester);
        result.TryGetError(out _).ShouldEqual(reserved);
        if (!reserved)
        {
            ((Contracts.Clients.Events.ClientRegistered)result.AsT0.First().Event).RegisteredAt.ShouldEqual(Now);
        }
    }
}
