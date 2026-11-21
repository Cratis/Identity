// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Identity.Clients.Registering;
using Cratis.Identity.Clients.RotatingSecret;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Clients;

public class when_producing_secret_facts : given.client_commands
{
    [Fact]
    async Task should_append_only_digest_facts_for_initial_and_rotated_secrets()
    {
        var command = new RegisterClient(new("spec-client"), _configuration, Plaintext);
        var batch = command.Handle(_descriptors, _clock, _digester).AsT0.ToArray();
        var id = batch[0].EventSourceId;
        batch.All(item => item.EventSourceId == id).ShouldBeTrue();
        Guid.TryParse(id.Value, out _).ShouldBeTrue();
        batch.Select(item => item.Event.GetType()).ShouldEqual([typeof(ClientRegistered), typeof(ClientRedirectUrisChanged), typeof(ClientSecretRotated)]);
        using var scenario = new EventScenario();
        var result = await scenario.When.ForEventSource(id).Events(batch[0].Event, batch.Skip(1).Select(item => item.Event).ToArray());
        result.Errors.ShouldBeEmpty();
        result.IsSuccess.ShouldBeTrue();
        foreach (var item in batch)
        {
            JsonSerializer.Serialize(item.Event, item.Event.GetType()).Contains(Plaintext, StringComparison.Ordinal).ShouldBeFalse();
            item.Event.ToString().Contains(Plaintext, StringComparison.Ordinal).ShouldBeFalse();
        }

        var rotated = new RotateSecret(_id, Plaintext, new(Now, Now)).Handle(_active, _digester, _clock, _secretOptions).AsT0.Single().Event;
        JsonSerializer.Serialize(rotated, rotated.GetType()).Contains(Plaintext, StringComparison.Ordinal).ShouldBeFalse();
        command.ToString().Contains(Plaintext, StringComparison.Ordinal).ShouldBeFalse();
        new RotateSecret(_id, Plaintext, new(Now, Now)).ToString().Contains(Plaintext, StringComparison.Ordinal).ShouldBeFalse();
        var initial = (ClientSecretRotated)batch[2].Event;
        initial.ActivatedAt.ShouldEqual(initial.PreviousValidUntil);
    }

    [Fact]
    void should_compute_a_keyed_hmac_in_the_contract_form()
    {
        var key = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var expected = WebEncoders.Base64UrlEncode(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"cratis.identity.client-secret.v1\0{Plaintext}")));
        _digester.Digest(Plaintext).Value.ShouldEqual($"v1.{expected}");
        SecretDigest.IsWellFormed(_digester.Digest(Plaintext).Value).ShouldBeTrue();
        var other = new ClientSecretDigester(Options.Create(new ClientSecretOptions { Key = Convert.ToBase64String(new byte[32]) }));
        other.Digest(Plaintext).Value.ShouldNotEqual(_digester.Digest(Plaintext).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("AA==")]
    void should_fail_closed_without_a_valid_server_key(string key)
    {
        var error = Catch.Exception(() => _ = new ClientSecretDigester(Options.Create(new ClientSecretOptions { Key = key })));
        error.ShouldBeOfExactType<InvalidOperationException>();
        error.Message.ShouldEqual("invalid_client_secret_key_configuration");
        new ClientSecretOptions { Key = key }.ToString().ShouldEqual("Client secret key [redacted]");
    }
}
