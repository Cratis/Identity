// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.given;

/// <summary>Reads only protocol error codes, never logs a credential-bearing response.</summary>
public static class OidcErrors
{
    public static async Task<string?> From(HttpResponseMessage response)
    {
        if (response.Headers.Location is { IsAbsoluteUri: true } location) return QueryHelpers.ParseQuery(location.Query)["error"].ToString();
        if (response.Content.Headers.ContentType?.MediaType != "application/json") return null;
        var body = await Browser.ReadJson(response);
        return body.TryGetProperty("error", out var error) ? error.GetString() : null;
    }
}
