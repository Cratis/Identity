// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>The canonical, ASCII-only boundary before metadata lookup or network activity.</summary>
internal static class ClientIdUrlPolicy
{
    /// <summary>Tests an exact identifier without normalizing its identity.</summary>
    /// <param name="value">The untrusted identifier.</param>
    /// <returns>Whether the identifier can be fetched.</returns>
    public static bool IsValid(string value) => IsPublicHttps(value, out var uri) && uri.AbsolutePath.Length > 1;

    /// <summary>Tests a canonical public HTTPS URL, also used for web redirect declarations.</summary>
    /// <param name="value">The URL.</param>
    /// <param name="uri">The parsed URL, never a replacement identity.</param>
    /// <returns>Whether the URL is canonical and contains no credentials, query or fragment.</returns>
    internal static bool IsPublicHttps(string value, out Uri uri)
    {
        uri = null!;
        if (!HasCanonicalCharacters(value) || !Uri.TryCreate(value, UriKind.Absolute, out var parsed)) return false;
        if (!HasPublicAuthority(parsed) || !HasCanonicalPath(value, parsed)) return false;
        uri = parsed;
        return true;
    }

    static bool HasCanonicalCharacters(string value) => !string.IsNullOrEmpty(value) && value.Length <= 2048 &&
        value.All(_ => _ > ' ' && _ < 127 && _ != '\\') && !value.Contains('?') && !value.Contains('#');

    static bool HasPublicAuthority(Uri uri) => uri.Scheme == Uri.UriSchemeHttps && uri.Port == 443 && uri.UserInfo.Length == 0 && IsPublicHost(uri);

    static bool HasCanonicalPath(string value, Uri uri)
    {
        if (!string.Equals(uri.AbsoluteUri, value, StringComparison.Ordinal)) return false;
        var path = Uri.UnescapeDataString(value[value.IndexOf('/', 8)..]);
        return !path.Split('/').Any(_ => _ == "." || _ == "..");
    }

    static bool IsPublicHost(Uri uri)
    {
        var host = uri.IdnHost;
        var labels = host.Split('.');
        return uri.HostNameType == UriHostNameType.Dns && !IPAddress.TryParse(host, out _) && labels.Length > 1 &&
            labels.All(IsDnsLabel) && char.IsAsciiLetter(labels[^1][0]) &&
            !host.EndsWith(".localhost", StringComparison.Ordinal) && !host.EndsWith(".local", StringComparison.Ordinal) && !host.EndsWith(".internal", StringComparison.Ordinal);
    }

    static bool IsDnsLabel(string label) => label.Length is > 0 and <= 63 && char.IsAsciiLetterOrDigit(label[0]) && char.IsAsciiLetterOrDigit(label[^1]) && label.All(_ => char.IsAsciiLetterOrDigit(_) || _ == '-');
}
