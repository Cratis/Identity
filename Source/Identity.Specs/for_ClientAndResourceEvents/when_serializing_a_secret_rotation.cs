// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;
using Cratis.Identity.Replay.for_EventLog.given;

namespace Cratis.Identity.for_ClientAndResourceEvents;

public class when_serializing_a_secret_rotation : Specification
{
    const string Digest = "v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    readonly ClientSecretRotated _event = new(ClientSecretId.New(), new SecretDigest(Digest), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

    [Fact]
    void should_preserve_the_no_overlap_deadline()
    {
        var restored = JsonSerializer.Deserialize<ClientSecretRotated>(JsonSerializer.Serialize(_event));
        restored.PreviousValidUntil.ShouldEqual(restored.ActivatedAt);
        typeof(ClientSecretRotated).GetProperty(nameof(ClientSecretRotated.PreviousValidUntil)).PropertyType.ShouldEqual(typeof(DateTimeOffset));
    }

    [Fact]
    void should_not_print_digest_material_from_the_event() => _event.ToString().Contains(Digest, StringComparison.Ordinal).ShouldBeFalse();

    [Fact]
    void should_not_print_secret_material_from_the_concept() => Secrets.All.Any(secret => _event.ClientSecretHash.ToString().Contains(secret, StringComparison.Ordinal)).ShouldBeFalse();

    [Fact]
    void should_have_only_the_single_string_digest_value() => typeof(SecretDigest).GetProperties().Select(property => property.Name).ShouldEqual(["Value"]);

    [Fact]
    void should_allow_only_reviewed_non_redeemable_leaves_in_the_payload()
    {
        var json = JsonSerializer.Serialize(new { eventType = typeof(ClientSecretRotated).FullName, content = _event });
        EventLogScanner.Findings([json], Secrets.All).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("synthetic-plaintext-secret")]
    [InlineData("c3ludGhldGljLXBsYWludGV4dC1zZWNyZXQ=")]
    [InlineData("v0.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB")]
    [InlineData("v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    public void should_reject_non_digest_input_without_printing_it(string input)
    {
        var error = Catch.Exception(() => _ = new SecretDigest(input));
        error.ShouldBeOfExactType<ArgumentException>();
        error.Message.Contains(input, StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    void should_reject_the_existing_operational_pbkdf2_envelope()
    {
        var envelope = Protocol.Stores.ClientSecretHash.Create("synthetic-plaintext-secret");
        SecretDigest.IsWellFormed(envelope).ShouldBeFalse();
        Catch.Exception(() => _ = new SecretDigest(envelope)).ShouldBeOfExactType<ArgumentException>();
    }

    [Theory]
    [InlineData("ClientSecretHash", "synthetic-plaintext-secret")]
    [InlineData("ClientSecretId", "synthetic-plaintext-secret")]
    [InlineData("ClientSecretHash", "c3ludGhldGljLXBsYWludGV4dC1zZWNyZXQ=")]
    public void should_scan_for_raw_or_reversible_secrets(string member, string value)
    {
        var json = JsonSerializer.Serialize(new { eventType = typeof(ClientSecretRotated).FullName, content = new Dictionary<string, string> { [member] = value } });
        EventLogScanner.Findings([json], Secrets.All).ShouldContain((typeof(ClientSecretRotated).FullName!, "secret-property"));
    }

    [Fact]
    void should_not_exempt_an_envelope_or_a_different_event()
    {
        var envelope = JsonSerializer.Serialize(new { eventType = typeof(ClientSecretRotated).FullName, context = new { ClientSecretHash = Digest } });
        var otherEvent = JsonSerializer.Serialize(new { eventType = "OtherEvent", content = new { ClientSecretHash = Digest } });
        EventLogScanner.Findings([envelope, otherEvent], []).Count.ShouldEqual(2);
    }
}
