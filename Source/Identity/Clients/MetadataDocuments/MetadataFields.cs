// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Validates the supported, non-confidential metadata field subset.</summary>
internal static class MetadataFields
{
    /// <summary>Checks identity and security-sensitive declarations. Presentation URLs and scope are ignored.</summary>
    /// <param name="root">The document root.</param>
    /// <param name="clientId">The exact fetch identity.</param>
    /// <returns>A bounded refusal code.</returns>
    public static MetadataRefusal Validate(JsonElement root, string clientId)
    {
        if (String(root, "client_id") != clientId) return MetadataRefusal.Metadata;
        if (!ValidAuthentication(root)) return MetadataRefusal.UnsupportedAuthMethod;
        if (root.EnumerateObject().Any(_ => Forbidden(_.Name))) return MetadataRefusal.Metadata;
        if (!OptionalArray(root, "grant_types", [GrantTypes.AuthorizationCode, GrantTypes.RefreshToken]) ||
            !OptionalArray(root, "response_types", [ResponseTypes.Code], exact: true))
        {
            return MetadataRefusal.Metadata;
        }
        if (root.TryGetProperty("client_name", out var name) && name.ValueKind != JsonValueKind.String) return MetadataRefusal.Metadata;
        return MetadataRefusal.None;
    }

    /// <summary>Reads a string field without coercion.</summary>
    /// <param name="root">The object.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The string, or null for a missing or incorrectly typed field.</returns>
    public static string? String(JsonElement root, string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>Tests an explicitly declared grant, never granting refresh by default.</summary>
    /// <param name="root">The validated object.</param>
    /// <returns>Whether refresh was declared.</returns>
    public static bool AllowsRefresh(JsonElement root) => root.TryGetProperty("grant_types", out var grants) && grants.EnumerateArray().Any(_ => _.GetString() == GrantTypes.RefreshToken);

    static bool Forbidden(string name) => name.StartsWith("client_secret", StringComparison.Ordinal) || new[] { "registration_access_token", "jwks", "jwks_uri", "request_uris", "software_statement" }.Contains(name, StringComparer.Ordinal) ||
        name.StartsWith("request_object_", StringComparison.Ordinal) || name.StartsWith("id_token_", StringComparison.Ordinal) || name.StartsWith("userinfo_", StringComparison.Ordinal);

    static bool ValidAuthentication(JsonElement root)
    {
        if (root.TryGetProperty("token_endpoint_auth_method", out _) && String(root, "token_endpoint_auth_method") != "none") return false;
        if (!root.TryGetProperty("token_endpoint_auth_methods_supported", out var methods)) return true;
        return methods.ValueKind == JsonValueKind.Array && methods.GetArrayLength() == 1 && methods[0].ValueKind == JsonValueKind.String && methods[0].GetString() == "none";
    }

    static bool OptionalArray(JsonElement root, string name, string[] allowed, bool exact = false)
    {
        if (!root.TryGetProperty(name, out var values)) return true;
        if (values.ValueKind != JsonValueKind.Array) return false;
        var items = values.EnumerateArray().ToArray();
        return (!exact || items.Length == 1) && items.All(_ => _.ValueKind == JsonValueKind.String && allowed.Contains(_.GetString(), StringComparer.Ordinal)) &&
            items.Select(_ => _.GetString()).Distinct(StringComparer.Ordinal).Count() == items.Length;
    }
}
