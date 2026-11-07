// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Identity.Contracts.Check;
using Cratis.Identity.Contracts.Compliance;

namespace Cratis.Identity.for_EventContentRules;

public class when_checking_violating_events : Specification
{
    [Theory]
    [InlineData(typeof(TokenPayload), "AccessToken", "forbidden_member_name")]
    [InlineData(typeof(SecretPayload), "ClientSecret", "forbidden_member_name")]
    [InlineData(typeof(KeyPayload), "KeyMaterial", "forbidden_member_name")]
    [InlineData(typeof(BytesPayload), "Attachment", "forbidden_payload_type")]
    [InlineData(typeof(CryptoPayload), "Attachment", "forbidden_payload_type")]
    [InlineData(typeof(AliasedPayload), "Details.Value", "forbidden_member_name")]
    [InlineData(typeof(PersonalPayload), "Email", "missing_pii")]
    [InlineData(typeof(SubjectPayload), "IdpSub", "missing_pii")]
    [InlineData(typeof(JsonPayload), "Details", "open_payload_shape")]
    [InlineData(typeof(ObjectPayload), "Details", "open_payload_shape")]
    [InlineData(typeof(MapPayload), "Details", "open_payload_shape")]
    [InlineData(typeof(SecretConceptPayload), "Details", "forbidden_member_name")]
    [InlineData(typeof(StatePayload), "State", "forbidden_member_name")]
    [InlineData(typeof(PatPayload), "PAT", "forbidden_member_name")]
    [InlineData(typeof(CodePayload), "UserCode", "forbidden_member_name")]
    [InlineData(typeof(ReverseAliasPayload), "Password", "forbidden_member_name")]
    [InlineData(typeof(NullableCryptoPayload), "Attachment", "forbidden_payload_type")]
    [InlineData(typeof(MemoryPayload), "Attachment", "forbidden_payload_type")]
    [InlineData(typeof(ClaimsPayload), "Details", "forbidden_payload_type")]
    [InlineData(typeof(CredentialPayload), "Details", "forbidden_payload_type")]
    [InlineData(typeof(UntypedCollectionPayload), "Details", "open_payload_shape")]
    [InlineData(typeof(FieldPayload), "AccessToken", "forbidden_member_name")]
    [InlineData(typeof(ExtendedConceptPayload), "Details.AccessToken", "forbidden_member_name")]
    public void should_reject_the_payload(Type type, string path, string reason)
    {
        var shape = ReflectionShapeBuilder.Build(type, true);
        ContentRules.Check(shape, SafeConceptRegistry.Entries).ShouldContain(new Violation(path, reason));
    }

    [Fact]
    public void should_terminate_cycles_without_skipping_other_members()
    {
        var shape = ReflectionShapeBuilder.Build(typeof(CyclicPayload), true);
        ContentRules.Check(shape, SafeConceptRegistry.Entries).ShouldContain(new Violation("AccessToken", "forbidden_member_name"));
    }

    [Fact]
    public void should_not_print_values()
    {
        var instance = new TokenPayload("synthetic-value-not-for-diagnostics");
        var diagnostics = string.Join('\n', ContentRules.Check(ReflectionShapeBuilder.Build(instance.GetType(), true), SafeConceptRegistry.Entries));
        diagnostics.Contains(instance.AccessToken, StringComparison.Ordinal).ShouldBeFalse();
    }

    internal sealed record SecretConceptPayload(SecretText Details);
    internal sealed record SecretText(string Value) : Concepts.ConceptAs<string>(Value);
    internal sealed record StatePayload(string State);
    internal sealed record PatPayload(string PAT);
    internal sealed record CodePayload(string UserCode);
    internal sealed record ReverseAliasPayload([property: JsonPropertyName("Details")] string Password);
    internal sealed record NullableCryptoPayload(RSAParameters? Attachment);
    internal sealed record MemoryPayload(ReadOnlyMemory<byte> Attachment);
    internal sealed record ClaimsPayload(System.Security.Claims.ClaimsPrincipal Details);
    internal sealed record CredentialPayload(System.Net.NetworkCredential Details);
    internal sealed record UntypedCollectionPayload(System.Collections.ArrayList Details);
    internal sealed class FieldPayload
    {
        public string AccessToken = string.Empty;
    }
    internal sealed record ExtendedConceptPayload(ExtendedText Details);
    internal sealed record ExtendedText(string Value, string AccessToken) : Concepts.ConceptAs<string>(Value);
    internal sealed record TokenPayload(string AccessToken);
    internal sealed record SecretPayload(string ClientSecret);
    internal sealed record KeyPayload(string KeyMaterial);
    internal sealed record BytesPayload(byte[] Attachment);
    internal sealed record CryptoPayload(RSAParameters Attachment);
    internal sealed record AliasedPayload(Alias Details);
    internal sealed record Alias([property: JsonPropertyName("refresh_token")] string Value);
    internal sealed record PersonalPayload(string Email);
    internal sealed record SubjectPayload(string IdpSub);
    internal sealed record JsonPayload(JsonElement Details);
    internal sealed record ObjectPayload(object Details);
    internal sealed record MapPayload(Dictionary<string, string> Details);
    internal sealed record CyclicPayload(CyclicPayload Next, string AccessToken);
}
