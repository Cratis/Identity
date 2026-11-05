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
        if (!body.TryGetProperty("tenants", out var tenants) || tenants.ValueKind != JsonValueKind.Array ||
            tenants.GetArrayLength() > 256)
        {
            return null;
        }

        var result = new List<SelectableTenant>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tenant in tenants.EnumerateArray())
        {
            if (tenant.ValueKind != JsonValueKind.Object ||
                !tenant.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var tenantId = id.GetString()!;
            if (!TenantId.IsMatch(tenantId) || !ids.Add(tenantId))
            {
                return null;
            }

            var name = tenantId;
            if (tenant.TryGetProperty("name", out var displayName))
            {
                if (displayName.ValueKind != JsonValueKind.String)
                {
                    return null;
                }

                var displayNameValue = displayName.GetString()!;
                if (displayNameValue.Length > 128 || displayNameValue.Any(char.IsControl))
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(displayNameValue))
                {
                    name = displayNameValue;
                }
            }

            result.Add(new SelectableTenant(tenantId, name));
        }

        return result;
    }
}
