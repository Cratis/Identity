// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Device;

/// <summary>The same unambiguous 20-letter alphabet is used for generation and verification.</summary>
internal static class DeviceGrantUserCode
{
    /// <summary>Eight independent symbols provide more than 34 bits of entropy.</summary>
    internal const string Alphabet = "BCDFGHJKLMNPQRSTVWXZ";

    /// <summary>Normalizes presentation separators only, never ambiguous characters.</summary>
    /// <param name="value">The entered code.</param>
    /// <returns>The canonical code, or null for malformed input.</returns>
    /// <param name="length">The configured generated length.</param>
    /// <param name="alphabet">The configured generated alphabet.</param>
    internal static string? Normalize(string? value, int length = 8, string alphabet = Alphabet)
    {
        if (value is null || value.Length > length * 2) return null;
        var normalized = value.Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        return normalized.Length == length && normalized.All(alphabet.Contains) ? normalized : null;
    }
}
