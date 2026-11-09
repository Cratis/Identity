// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_validating_hostile_tokens : given.a_subject_token_validator
{
    SubjectTokenRefusal _criticalHeader;
    SubjectTokenRefusal _duplicateSubject;
    SubjectTokenRefusal _duplicateHeaderAlgorithm;
    SubjectTokenRefusal _kidOfAnotherKey;
    SubjectTokenRefusal _embeddedKey;
    SubjectTokenRefusal _numericSubject;
    SubjectTokenRefusal _emptySubject;
    SubjectTokenRefusal _stringIssuedAt;
    SubjectTokenRefusal _fractionalIssuedAt;
    SubjectTokenRefusal _numericTokenId;
    SubjectTokenRefusal _differentlyCasedIssuer;
    SubjectTokenRefusal _payloadNotJson;
    SubjectTokenRefusal _payloadNotAnObject;
    SubjectTokenRefusal _headerNotJson;
    SubjectTokenRefusal _truncatedSignature;
    SubjectTokenRefusal _emptyPayload;
    SubjectTokenRefusal _pointerHeaders;
    SubjectTokenRefusal _surrogateHeaderName;
    SubjectTokenRefusal _surrogateClaimName;
    SubjectTokenRefusal _surrogateClaimValue;
    SubjectTokenResult _withoutKeyId;

    async Task Because()
    {
        var rsa = ((RsaSecurityKey)issuer.RsaCredentials.Key).Rsa;
        var header = $"{{\"alg\":\"RS256\",\"typ\":\"JWT\",\"kid\":\"{ExternalIssuerFixture.RsaKeyId}\"";
        var payload = JsonSerializer.Serialize(Payload());

        _criticalHeader = await RefusalOf(Manual(header + ",\"crit\":[\"exp\"]}", payload, rsa));
        _duplicateSubject = await RefusalOf(Manual(header + "}", payload.TrimEnd('}') + ",\"sub\":\"someone-else\"}", rsa));
        _duplicateHeaderAlgorithm = await RefusalOf(Manual(header + ",\"alg\":\"RS256\"}", payload, rsa));
        _kidOfAnotherKey = await RefusalOf(Manual(header.Replace(ExternalIssuerFixture.RsaKeyId, ExternalIssuerFixture.EcKeyId) + "}", payload, rsa));

        using var attacker = RSA.Create(2048);
        var attackerKey = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["kty"] = "RSA",
            ["kid"] = "attacker",
            ["n"] = Base64UrlEncoder.Encode(attacker.ExportParameters(false).Modulus),
            ["e"] = Base64UrlEncoder.Encode(attacker.ExportParameters(false).Exponent)
        });
        _embeddedKey = await RefusalOf(Manual($"{{\"alg\":\"RS256\",\"typ\":\"JWT\",\"kid\":\"attacker\",\"jwk\":{attackerKey}}}", payload, attacker));

        _numericSubject = await RefusalOf(Manual(header + "}", Replace("sub", "12345"), rsa));
        _emptySubject = await RefusalOf(Manual(header + "}", Replace("sub", "\"\""), rsa));
        _stringIssuedAt = await RefusalOf(Manual(header + "}", Replace("iat", $"\"{time.Now.ToUnixTimeSeconds()}\""), rsa));
        _fractionalIssuedAt = await RefusalOf(Manual(header + "}", Replace("iat", $"{time.Now.ToUnixTimeSeconds()}.5"), rsa));
        _numericTokenId = await RefusalOf(Manual(header + "}", Replace("jti", "42"), rsa));
        _differentlyCasedIssuer = await RefusalOf(Manual(header + "}", Replace("iss", $"\"{Configurations.ExternalIssuer.ToUpperInvariant()}\""), rsa));

        _payloadNotJson = await RefusalOf(Manual(header + "}", "not json at all", rsa));
        _payloadNotAnObject = await RefusalOf(Manual(header + "}", "[1,2,3]", rsa));
        _emptyPayload = await RefusalOf(Manual(header + "}", string.Empty, rsa));
        _headerNotJson = await RefusalOf(Manual("{not json", payload, rsa));
        _surrogateHeaderName = await RefusalOf(Manual(header + ",\"\\ud800\":1}", payload, rsa));
        _surrogateClaimName = await RefusalOf(Manual(header + "}", payload.TrimEnd('}') + ",\"\\ud800\":1}", rsa));
        _surrogateClaimValue = await RefusalOf(Manual(header + "}", Replace("iss", "\"\\ud800\""), rsa));

        var valid = Valid().Split('.');
        _truncatedSignature = await RefusalOf($"{valid[0]}.{valid[1]}.{Base64UrlEncoder.Encode(new byte[100])}");

        // jku/x5u/x5c point at keys; they are never followed, and a good signature by a published key still counts.
        var pointers = header + ",\"jku\":\"https://attacker.example/jwks\",\"x5u\":\"https://attacker.example/cert\"}";
        _pointerHeaders = await RefusalOf(Manual(pointers, payload, rsa));
        _withoutKeyId = await Validate(Manual(JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" }), payload, rsa));
    }

    string Replace(string claim, string rawValue)
    {
        var payload = Payload();
        payload.Remove(claim);
        var json = JsonSerializer.Serialize(payload).TrimEnd('}');
        return $"{json},\"{claim}\":{rawValue}}}";
    }

    static string Manual(string headerJson, string payloadJson, RSA rsa)
    {
        var signingInput = $"{Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(headerJson))}.{Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(payloadJson))}";
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{signingInput}.{Base64UrlEncoder.Encode(signature)}";
    }

    [Fact] void should_refuse_a_critical_header() => _criticalHeader.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_a_duplicated_claim() => _duplicateSubject.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_a_duplicated_header_parameter() => _duplicateHeaderAlgorithm.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_a_kid_that_names_another_key() => _kidOfAnotherKey.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_a_token_signed_by_a_key_it_embeds() => _embeddedKey.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_a_numeric_subject() => _numericSubject.ShouldNotEqual(SubjectTokenRefusal.None);
    [Fact] void should_refuse_an_empty_subject() => _emptySubject.ShouldEqual(SubjectTokenRefusal.MissingClaim);
    [Fact] void should_refuse_a_string_issued_at() => _stringIssuedAt.ShouldNotEqual(SubjectTokenRefusal.None);
    [Fact] void should_refuse_a_fractional_issued_at() => _fractionalIssuedAt.ShouldNotEqual(SubjectTokenRefusal.None);
    [Fact] void should_refuse_a_numeric_token_id() => _numericTokenId.ShouldNotEqual(SubjectTokenRefusal.None);
    [Fact] void should_refuse_an_issuer_that_differs_only_in_case() => _differentlyCasedIssuer.ShouldEqual(SubjectTokenRefusal.UnknownIssuer);
    [Fact] void should_refuse_a_payload_that_is_not_json() => _payloadNotJson.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_a_payload_that_is_not_an_object() => _payloadNotAnObject.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_an_empty_payload() => _emptyPayload.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_a_header_that_is_not_json() => _headerNotJson.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_an_unpaired_surrogate_in_a_header_parameter_name() => _surrogateHeaderName.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_an_unpaired_surrogate_in_a_claim_name() => _surrogateClaimName.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_an_unpaired_surrogate_in_a_claim_value() => _surrogateClaimValue.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_a_truncated_signature() => _truncatedSignature.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_ignore_key_pointer_headers() => _pointerHeaders.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_accept_a_token_without_a_key_id_signed_by_a_published_key() => _withoutKeyId.Refusal.ShouldEqual(SubjectTokenRefusal.None);
}
