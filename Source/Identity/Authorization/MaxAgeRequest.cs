// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Detects an explicit maximum-age parameter on an authorization request.
/// </summary>
internal static class MaxAgeRequest
{
    /// <summary>
    /// Checks the query and, when applicable, the posted form for the parameter.
    /// </summary>
    /// <param name="request">The current HTTP request, if available.</param>
    /// <returns>Whether the request contains the maximum-age parameter.</returns>
    internal static async ValueTask<bool> IsPresent(HttpRequest? request)
    {
        if (request is null) return false;
        if (HasMaxAge(request.Query.Keys)) return true;
        if (!HttpMethods.IsPost(request.Method) || !request.HasFormContentType) return false;

        var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);
        return HasMaxAge(form.Keys);
    }

    static bool HasMaxAge(IEnumerable<string> keys) => keys.Any(key => string.Equals(key, Parameters.MaxAge, StringComparison.OrdinalIgnoreCase));
}
