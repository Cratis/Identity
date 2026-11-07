// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.DPoP;

/// <summary>Validates bounded compact ES256 token-endpoint proofs without network key resolution.</summary>
/// <param name="time">The clock shared by proof and nonce checks.</param>
internal sealed class DPoPProofValidator(TimeProvider time)
{
    static readonly string[] _forbiddenKeyMembers = ["d", "k", "jku", "x5u", "x5c"];

    /// <summary>Validates exactly one header against the exact configured endpoint.</summary>
    /// <param name="headers">The DPoP header values.</param>
    /// <param name="endpoint">The configured public endpoint, not a request-derived URL.</param>
    /// <returns>A verified proof or a non-sensitive reason.</returns>
    internal (DPoPProof? Proof, DPoPFailure Failure) Validate(IReadOnlyList<string> headers, string endpoint)
    {
        if (headers.Count == 0) return (null, DPoPFailure.MissingProof);
        if (headers.Count != 1) return (null, DPoPFailure.Malformed);
        if (headers[0].Length > 4096) return (null, DPoPFailure.Malformed);
        try
        {
            return ValidateCompact(headers[0], endpoint);
        }
        catch (Exception exception) when (InvalidInput(exception))
        {
            return (null, DPoPFailure.Malformed);
        }
    }

    static bool InvalidInput(Exception exception) => exception is JsonException or FormatException or ArgumentException or CryptographicException;

    static bool CompactShape(string[] parts) => parts.Length == 3 && parts.All(part => part.Length > 0 && part.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));

    static DPoPFailure HeaderFailure(JsonElement header)
    {
        if (Text(header, "typ") != "dpop+jwt") return DPoPFailure.Malformed;
        if (header.TryGetProperty("crit", out _)) return DPoPFailure.Malformed;
        if (Text(header, "alg") != "ES256") return DPoPFailure.UnsupportedAlgorithm;
        if (HasForbiddenKeyMaterial(header)) return DPoPFailure.InvalidJwk;
        if (!header.TryGetProperty("jwk", out var jwk)) return DPoPFailure.InvalidJwk;
        return ValidKey(jwk) ? DPoPFailure.None : DPoPFailure.InvalidJwk;
    }

    static bool ValidKey(JsonElement jwk)
    {
        if (jwk.ValueKind != JsonValueKind.Object) return false;
        if (Text(jwk, "kty") != "EC") return false;
        if (Text(jwk, "crv") != "P-256") return false;
        if (HasForbiddenKeyMaterial(jwk)) return false;
        return CoordinatesPresent(jwk) && KeyUsageCompatible(jwk);
    }

    static bool CoordinatesPresent(JsonElement jwk) => Text(jwk, "x") is not null && Text(jwk, "y") is not null;

    static bool KeyUsageCompatible(JsonElement jwk) => OptionalString(jwk, "use", "sig") && OptionalString(jwk, "alg", "ES256") && VerificationOperations(jwk);

    static bool OptionalString(JsonElement value, string member, string expected) => !value.TryGetProperty(member, out _) || Text(value, member) == expected;

    static bool VerificationOperations(JsonElement jwk)
    {
        if (!jwk.TryGetProperty("key_ops", out var operations)) return true;
        if (operations.ValueKind != JsonValueKind.Array) return false;
        if (operations.GetArrayLength() != 1) return false;
        return operations[0].ValueKind == JsonValueKind.String && operations[0].GetString() == "verify";
    }

    static bool ValidCoordinate(byte[] coordinate, string? encoded) => coordinate.Length == 32 && Base64UrlEncoder.Encode(coordinate) == encoded;

    static bool HasForbiddenKeyMaterial(JsonElement value) => _forbiddenKeyMembers.Any(member => value.TryGetProperty(member, out _));

    static string? Text(JsonElement value, string member) => value.TryGetProperty(member, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;

    static string Thumbprint(JsonElement jwk)
    {
        var canonical = $"{{\"crv\":\"P-256\",\"kty\":\"EC\",\"x\":\"{Text(jwk, "x")}\",\"y\":\"{Text(jwk, "y")}\"}}";
        return Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    static bool ValidSignature(string[] parts, ECDsa key)
    {
        var signature = Base64UrlEncoder.DecodeBytes(parts[2]);
        return signature.Length == 64 && key.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    static (DPoPProof?, DPoPFailure) Verify(string[] parts, JsonElement jwk, JsonElement payload)
    {
        try
        {
            var x = Base64UrlEncoder.DecodeBytes(Text(jwk, "x"));
            var y = Base64UrlEncoder.DecodeBytes(Text(jwk, "y"));
            if (!ValidCoordinate(x, Text(jwk, "x"))) return (null, DPoPFailure.InvalidJwk);
            if (!ValidCoordinate(y, Text(jwk, "y"))) return (null, DPoPFailure.InvalidJwk);
            using var key = ECDsa.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = new ECPoint { X = x, Y = y } });
            var proof = new DPoPProof(Thumbprint(jwk), Text(payload, "jti") ?? throw new JsonException(), Text(payload, "nonce"));
            return ValidSignature(parts, key) ? (proof, DPoPFailure.None) : (null, DPoPFailure.InvalidSignature);
        }
        catch (Exception exception) when (InvalidInput(exception))
        {
            return (null, DPoPFailure.InvalidJwk);
        }
    }

    static JsonDocument Parse(string encoded)
    {
        var bytes = Base64UrlEncoder.DecodeBytes(encoded);
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { MaxDepth = 8 });
        var objects = new Stack<HashSet<string>>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject) objects.Push(new(StringComparer.Ordinal));
            else if (reader.TokenType == JsonTokenType.EndObject) objects.Pop();
            else if (reader.TokenType == JsonTokenType.PropertyName && !objects.Peek().Add(reader.GetString()!)) throw new JsonException();
        }

        return JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
    }

    (DPoPProof?, DPoPFailure) ValidateCompact(string compact, string endpoint)
    {
        var parts = compact.Split('.');
        if (!CompactShape(parts)) return (null, DPoPFailure.Malformed);
        using var header = Parse(parts[0]);
        using var payload = Parse(parts[1]);
        if (header.RootElement.ValueKind != JsonValueKind.Object || payload.RootElement.ValueKind != JsonValueKind.Object) return (null, DPoPFailure.Malformed);
        var failure = HeaderFailure(header.RootElement);
        if (failure != DPoPFailure.None) return (null, failure);
        failure = PayloadFailure(payload.RootElement, endpoint);
        if (failure != DPoPFailure.None) return (null, failure);
        return Verify(parts, header.RootElement.GetProperty("jwk"), payload.RootElement);
    }

    DPoPFailure PayloadFailure(JsonElement payload, string endpoint)
    {
        if (Text(payload, "htm") != "POST") return DPoPFailure.HtmMismatch;
        if (!string.Equals(Text(payload, "htu"), endpoint, StringComparison.Ordinal)) return DPoPFailure.HtuMismatch;
        if (!ValidTimestamp(payload)) return DPoPFailure.IatOutsideWindow;
        if (Text(payload, "jti") is not { Length: > 0 and <= 256 }) return DPoPFailure.InvalidJti;
        if (payload.TryGetProperty("nonce", out var nonce) && nonce.ValueKind != JsonValueKind.String) return DPoPFailure.Malformed;
        return DPoPFailure.None;
    }

    bool ValidTimestamp(JsonElement payload)
    {
        if (!payload.TryGetProperty("iat", out var stamp)) return false;
        if (stamp.ValueKind != JsonValueKind.Number) return false;
        if (!stamp.TryGetInt64(out var issuedAt)) return false;
        var now = time.GetUtcNow().ToUnixTimeSeconds();
        return issuedAt > now - 300 && issuedAt <= now + 30;
    }
}
