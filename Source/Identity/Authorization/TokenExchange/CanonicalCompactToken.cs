// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Text;
using System.Text.Json;

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Checks that a compact JWS has exactly one encoding, so that no variant of an accepted token can pass as a new one:
/// three segments, strict unpadded base64url with no stray characters or non-zero trailing bits, JSON objects without
/// duplicated members, a <c language="text">crit</c> header refused (no extension is understood), and registered claims
/// of the type the validator reads.
/// </summary>
internal static class CanonicalCompactToken
{
    const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    static readonly string[] _numericClaims = ["iat", "exp", "nbf"];
    static readonly string[] _stringClaims = ["iss", "sub", "jti"];

    /// <summary>Checks the token before any verification.</summary>
    /// <param name="rawToken">The raw compact serialization.</param>
    /// <returns>True when the token is in its one canonical form.</returns>
    public static bool IsCanonical(string rawToken)
    {
        var segments = rawToken.Split('.');
        if (segments.Length != 3)
        {
            return false;
        }

        if (segments.Take(2).Any(string.IsNullOrEmpty))
        {
            return false;
        }

        var decoded = new byte[2][];
        for (var i = 0; i < 2; i++)
        {
            if (!TryDecode(segments[i], out decoded[i]))
            {
                return false;
            }
        }

        // The signature may be empty (the verifier refuses it) but never encoded two ways.
        return TryDecode(segments[2], out _) && IsHeader(decoded[0]) && IsClaims(decoded[1]);
    }

    static bool TryDecode(string segment, out byte[] bytes)
    {
        bytes = [];
        if (segment.Any(_ => !Alphabet.Contains(_)))
        {
            return false;
        }

        try
        {
            bytes = Base64Url.DecodeFromChars(segment);
            return string.Equals(Base64Url.EncodeToString(bytes), segment, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    static bool IsHeader(byte[] json) => TryParse(json, out var root) && !root.TryGetProperty("crit", out _);

    static bool IsClaims(byte[] json) =>
        TryParse(json, out var root) &&
        _stringClaims.All(_ => !root.TryGetProperty(_, out var value) || value.ValueKind == JsonValueKind.String) &&
        _numericClaims.All(_ => !root.TryGetProperty(_, out var value) || IsNumericDate(value)) &&
        (!root.TryGetProperty("aud", out var audience) || IsAudience(audience));

    /// <summary>
    /// RFC 7519 permits fractional seconds. Bound dates through 9999-12-31T23:59:59Z,
    /// before converting to DateTimeOffset; overflow exponents must never become infinity.
    /// </summary>
    /// <param name="value">The JSON claim.</param>
    /// <returns>Whether the value is a finite, non-negative NumericDate within the supported range.</returns>
    static bool IsNumericDate(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var seconds))
        {
            return false;
        }

        return double.IsFinite(seconds) && seconds is >= 0 and <= 253_402_300_799 && !IsNegative(value.GetRawText());
    }

    static bool IsNegative(string number)
    {
        if (number[0] != '-')
        {
            return false;
        }

        // A negative number can underflow to -0 as a double; inspect the significand too.
        var exponent = number.IndexOfAny(['e', 'E']);
        var significand = exponent < 0 ? number.AsSpan() : number.AsSpan(0, exponent);
        return significand.ContainsAnyInRange('1', '9');
    }

    static bool IsAudience(JsonElement audience) =>
        audience.ValueKind == JsonValueKind.String ||
        (audience.ValueKind == JsonValueKind.Array && audience.EnumerateArray().All(_ => _.ValueKind == JsonValueKind.String));

    static bool TryParse(byte[] json, out JsonElement root)
    {
        root = default;
        try
        {
            var document = JsonDocument.Parse(json);
            root = document.RootElement.Clone();
            document.Dispose();
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            return root.EnumerateObject().All(_ => names.Add(_.Name)) && AreAllStringsDecodable(root);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // A name or value with an unpaired surrogate escape or invalid UTF-8 cannot be decoded: refuse, never throw.
            return false;
        }
    }

    /// <summary>
    /// Parsing is lazy about string content; decodes every name and value now so a hostile one is refused here
    /// rather than throwing later when a claim is read.
    /// </summary>
    /// <param name="element">The element to walk.</param>
    /// <returns>True when every string decodes; a hostile one throws and is caught by the caller.</returns>
    static bool AreAllStringsDecodable(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                _ = element.GetString();
                return true;
            case JsonValueKind.Array:
                return element.EnumerateArray().All(AreAllStringsDecodable);
            case JsonValueKind.Object:
                return element.EnumerateObject().Select(property => (property.Name, property.Value)).All(_ => AreAllStringsDecodable(_.Value));
            default:
                return true;
        }
    }
}
