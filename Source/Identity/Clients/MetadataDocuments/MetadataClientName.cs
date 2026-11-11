// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Bounds untrusted presentation text without conveying client authority.</summary>
internal static class MetadataClientName
{
    /// <summary>Removes controls and directional/zero-width formatting, collapses whitespace and limits to 100 characters.</summary>
    /// <param name="name">The self-asserted name.</param>
    /// <param name="host">The ASCII fallback host.</param>
    /// <returns>Plain text only, still requiring HTML encoding when rendered.</returns>
    public static string Sanitize(string? name, string host)
    {
        if (name is null) return host;
        var clean = new string(name.Where(_ => !char.IsControl(_) && _ is not (>= '\u202a' and <= '\u202e') and not (>= '\u2066' and <= '\u2069') and not (>= '\u200b' and <= '\u200f')).ToArray());
        var collapsed = string.Join(' ', clean.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length == 0) return host;
        var length = Math.Min(100, collapsed.Length);
        if (char.IsHighSurrogate(collapsed[length - 1])) length--;
        return collapsed[..length];
    }
}
