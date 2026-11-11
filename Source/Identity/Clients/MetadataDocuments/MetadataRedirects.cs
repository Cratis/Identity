// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Clients.Registering;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Admits one unambiguous web or native redirect set; never drops invalid declarations.</summary>
internal static class MetadataRedirects
{
    /// <summary>Parses the complete declared allowlist.</summary>
    /// <param name="root">The document.</param>
    /// <param name="options">The issuer policy.</param>
    /// <param name="redirects">The admitted set.</param>
    /// <param name="native">Whether every declaration is native loopback.</param>
    /// <returns>Whether the whole set is admitted.</returns>
    public static bool TryParse(JsonElement root, ClientIdMetadataOptions options, out Uri[] redirects, out bool native)
    {
        redirects = [];
        native = false;
        if (!TryReadStrings(root, options.MaxRedirectUris, out var texts)) return false;
        native = texts.All(text => IsNative(text, options.AllowLocalhostLoopback));
        if (!native && !texts.All(text => ClientIdUrlPolicy.IsPublicHttps(text, out _))) return false;
        if (!HasExpectedType(root, native)) return false;
        if (native && texts.Any(_ => _.StartsWith("http://localhost/", StringComparison.Ordinal)))
        {
            // The opt-in localhost exception is CIMD-native only; legacy redirect-rule semantics stay unchanged.
            redirects = texts.Select(_ => new Uri(_)).ToArray();
            return true;
        }

        return ClientRedirectRules.TryParse(texts, native ? ClientApplicationType.Native : ClientApplicationType.Web, out redirects);
    }

    static bool TryReadStrings(JsonElement root, int maximum, out string[] texts)
    {
        texts = [];
        if (!root.TryGetProperty("redirect_uris", out var values) || values.ValueKind != JsonValueKind.Array) return false;
        if (values.GetArrayLength() < 1 || values.GetArrayLength() > maximum) return false;
        var items = values.EnumerateArray().ToArray();
        if (items.Any(_ => _.ValueKind != JsonValueKind.String)) return false;
        texts = items.Select(_ => _.GetString()!).ToArray();
        return texts.Distinct(StringComparer.Ordinal).Count() == texts.Length;
    }

    static bool HasExpectedType(JsonElement root, bool native) => !root.TryGetProperty("application_type", out _) || MetadataFields.String(root, "application_type") == (native ? "native" : "web");

    static bool UnsafeCharacter(char value) => char.IsControl(value) || char.IsWhiteSpace(value) || value == '\\';

    static bool HasNativeSyntax(string text) => text.Length <= 2048 && !text.Any(UnsafeCharacter) && !text.Contains('?') && !text.Contains('#');

    static bool IsNative(string text, bool allowLocalhost)
    {
        if (!HasNativeSyntax(text)) return false;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.AbsoluteUri != text) return false;
        return text.StartsWith("http://127.0.0.1/", StringComparison.Ordinal) || text.StartsWith("http://[::1]/", StringComparison.Ordinal) ||
            (allowLocalhost && text.StartsWith("http://localhost/", StringComparison.Ordinal));
    }
}
