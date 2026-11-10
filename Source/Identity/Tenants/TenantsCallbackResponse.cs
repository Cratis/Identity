// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cratis.Identity.Tenants;

internal static partial class TenantsCallbackResponse
{
    // Absolute anchors also reject a trailing newline (unlike the regex $ anchor).
    [GeneratedRegex(@"\A[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TenantId { get; }

    /// <summary>
    /// Defensively reads the internal callback answer without normalizing product identifiers.
    /// </summary>
    /// <param name="body">The bounded JSON response with its subject already validated.</param>
    /// <returns>The selectable tenants, or null for a malformed answer.</returns>
    internal static IEnumerable<SelectableTenant>? Read(JsonElement body)
    {
        if (!body.TryGetProperty("tenants", out var tenants) || !ValidTenantArray(tenants))
        {
            return null;
        }

        var result = new List<SelectableTenant>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tenant in tenants.EnumerateArray())
        {
            var selectable = ReadTenant(tenant, ids);
            if (selectable is null) return null;
            result.Add(selectable);
        }

        return result;
    }

    static bool ValidTenantArray(JsonElement tenants) =>
        tenants.ValueKind == JsonValueKind.Array && tenants.GetArrayLength() <= 256;

    static SelectableTenant? ReadTenant(JsonElement tenant, HashSet<string> ids)
    {
        if (tenant.ValueKind != JsonValueKind.Object) return null;
        if (!tenant.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var tenantId = id.GetString()!;
        if (!TenantId.IsMatch(tenantId) || !ids.Add(tenantId))
        {
            return null;
        }

        var name = ReadName(tenant, tenantId);
        return name is null ? null : new SelectableTenant(tenantId, name);
    }

    static string? ReadName(JsonElement tenant, string tenantId)
    {
        if (!tenant.TryGetProperty("name", out var displayName)) return tenantId;
        if (displayName.ValueKind != JsonValueKind.String) return null;

        var displayNameValue = displayName.GetString()!;
        if (displayNameValue.Length > 128 || displayNameValue.Any(char.IsControl)) return null;
        return string.IsNullOrWhiteSpace(displayNameValue) ? tenantId : displayNameValue;
    }
}
