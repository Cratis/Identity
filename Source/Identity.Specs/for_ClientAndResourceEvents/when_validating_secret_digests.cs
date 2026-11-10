// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Concepts;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;
using Cratis.Json;

namespace Cratis.Identity.for_ClientAndResourceEvents;

public class when_validating_secret_digests : Specification
{
    const string Digest = "v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    const string Plaintext = "synthetic-plaintext-secret";
    readonly SecretDigest _digest = new(Digest);

    [Theory]
    [InlineData(Plaintext)]
    [InlineData("c3ludGhldGljLXBsYWludGV4dC1zZWNyZXQ=")]
    public void should_reject_a_with_initializer_at_the_event_boundary(string input)
    {
        var invalid = _digest with { Value = input };
        Catch.Exception(() => _ = Rotation(invalid)).ShouldBeOfExactType<ArgumentException>();
    }

    [Theory]
    [InlineData(Plaintext)]
    [InlineData("c3ludGhldGljLXBsYWludGV4dC1zZWNyZXQ=")]
    public void should_reject_an_object_initializer_at_the_event_boundary(string input)
    {
        var invalid = new SecretDigest(Digest) { Value = input };
        Catch.Exception(() => _ = Rotation(invalid)).ShouldBeOfExactType<ArgumentException>();
    }

    [Fact]
    void should_reject_an_event_initializer_bypass()
    {
        var valid = Rotation(_digest);
        var invalid = _digest with { Value = Plaintext };
        Catch.Exception(() => _ = valid with { ClientSecretHash = invalid }).ShouldBeOfExactType<ArgumentException>();
    }

    [Fact]
    void should_reject_default_serialization_of_an_invalid_standalone_digest()
    {
        var invalid = (SecretDigest)((ConceptAs<string>)_digest with { Value = Plaintext });
        var error = Catch.Exception(() => _ = JsonSerializer.Serialize(invalid));
        error.ShouldBeOfExactType<ArgumentException>();
        error.Message.Contains(Plaintext, StringComparison.Ordinal).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_reject_an_event_holding_a_copy_changed_through_the_base_concept(bool conceptConverter)
    {
        var invalid = (SecretDigest)((ConceptAs<string>)_digest with { Value = Plaintext });
        Catch.Exception(() => _ = JsonSerializer.Serialize(Rotation(invalid), Options(conceptConverter))).ShouldBeOfExactType<ArgumentException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_reject_deserializing_plaintext(bool conceptConverter)
    {
        var json = conceptConverter ? JsonSerializer.Serialize(Plaintext) : JsonSerializer.Serialize(new { Value = Plaintext });
        Catch.Exception(() => _ = JsonSerializer.Deserialize<SecretDigest>(json, Options(conceptConverter))).ShouldNotBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_a_valid_copy_and_its_single_value(bool conceptConverter)
    {
        var valid = _digest with { Value = "v2.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" };
        var options = Options(conceptConverter);
        var restored = JsonSerializer.Deserialize<SecretDigest>(JsonSerializer.Serialize(valid, options), options);
        restored.Value.ShouldEqual(valid.Value);
        restored.ShouldEqual(valid);
        typeof(SecretDigest).GetProperties().Select(property => property.Name).ShouldEqual(["Value"]);
        var rotation = Rotation(valid);
        JsonSerializer.Deserialize<ClientSecretRotated>(JsonSerializer.Serialize(rotation, options), options).ClientSecretHash.ShouldEqual(valid);
    }

    static ClientSecretRotated Rotation(SecretDigest digest) => new(ClientSecretId.New(), digest, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

    static JsonSerializerOptions Options(bool conceptConverter)
    {
        var options = new JsonSerializerOptions();
        if (conceptConverter) options.Converters.Add(new ConceptAsJsonConverterFactory());
        return options;
    }
}
