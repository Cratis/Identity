// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;
using JsonDocument = System.Text.Json.JsonDocument;

namespace Cratis.Identity.Integration.given;

/// <summary>Reads only protocol error codes, never logs a credential-bearing response.</summary>
public static class OidcErrors
{
    public static async Task<string?> From(HttpResponseMessage response)
    {
        if (response.Headers.Location is { IsAbsoluteUri: true } location) return QueryHelpers.ParseQuery(location.Query)["error"].ToString();
        var text = await response.Content.ReadAsStringAsync();
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            using var body = JsonDocument.Parse(text);
            return body.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
        }
        if (response.Content.Headers.ContentType?.MediaType != "text/plain") return null;
        const string prefix = "error:";
        var line = text.Split('\n', StringSplitOptions.TrimEntries).FirstOrDefault(line => line.StartsWith(prefix, StringComparison.Ordinal));
        return line?[prefix.Length..].Trim();
    }
}
