// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Resources;

/// <summary>Closed resource catalogue syntax from identity-contract-v1 section 4.</summary>
internal static class ResourceRegistrationValidation
{
    /// <summary>Normalizes an admitted HTTPS indicator without credentials, query or fragment.</summary>
    /// <param name="value">The proposed indicator.</param>
    /// <returns>The canonical indicator, or null.</returns>
    public static string? NormalizeIndicator(string? value)
    {
        if (!IsIndicatorText(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !IsSafeUri(uri))
        {
            return null;
        }

        return CanonicalIndicator(uri);
    }

    /// <summary>Validates the normalized resource identity and its non-person label.</summary>
    /// <param name="indicator">The normalized indicator, or null on refusal.</param>
    /// <param name="audience">The audience.</param>
    /// <param name="prefix">The scope namespace.</param>
    /// <param name="label">The business label.</param>
    /// <returns>Whether every registration field is admitted.</returns>
    public static bool IsRegistration(string? indicator, string? audience, string? prefix, string? label) =>
        indicator is not null && IsSegment(audience) && IsSegment(prefix) && IsLabel(label);

    /// <summary>Requires a bounded lowercase catalogue segment, without normalization.</summary>
    /// <param name="value">The audience, prefix or operation.</param>
    /// <returns>Whether the segment is admitted.</returns>
    public static bool IsSegment(string? value) => value is { Length: > 0 and <= 64 } &&
        value[0] is >= 'a' and <= 'z' && value[^1] != '-' && !value.Contains("--", StringComparison.Ordinal) &&
        value.All(character => character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-');

    /// <summary>Requires an exact prefixed, non-administrative operation.</summary>
    /// <param name="scope">The proposed full scope name.</param>
    /// <param name="prefix">The registered resource prefix.</param>
    /// <returns>Whether the scope belongs to this resource.</returns>
    public static bool IsScope(string? scope, string prefix)
    {
        if (scope?.StartsWith($"{prefix}:", StringComparison.Ordinal) != true)
        {
            return false;
        }

        var operation = scope[(prefix.Length + 1)..];
        return IsSegment(operation) && operation is not ("admin" or "approve");
    }

    /// <summary>Bounds non-person business labels; controls and blank labels are refused.</summary>
    /// <param name="value">The proposed business text.</param>
    /// <returns>Whether the text is admitted.</returns>
    public static bool IsLabel(string? value) => value is { Length: > 0 and <= 256 } &&
        !string.IsNullOrWhiteSpace(value) && value.All(character => !char.IsControl(character));

    static string? CanonicalIndicator(Uri uri)
    {
        try
        {
            var canonical = new UriBuilder(uri)
            {
                Host = CanonicalHost(uri),
                Port = uri.IsDefaultPort ? -1 : uri.Port
            };
            return canonical.Uri.AbsoluteUri.TrimEnd('/');
        }
        catch (Exception exception) when (exception is UriFormatException or ArgumentException)
        {
            return null;
        }
    }

    static string CanonicalHost(Uri uri)
    {
        var host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        return host.Length == 0 ? throw new UriFormatException() : host;
    }

    static bool IsIndicatorText(string? value) => value is { Length: > 0 and <= 2048 } &&
        !value.Any(char.IsWhiteSpace) && value.IndexOfAny(['\\', '?', '#']) < 0;

    static bool IsSafeUri(Uri uri) => uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrEmpty(uri.Host) &&
        uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
        !uri.AbsolutePath.Contains("//", StringComparison.Ordinal);
}
