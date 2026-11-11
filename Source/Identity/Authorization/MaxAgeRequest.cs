// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

internal static class MaxAgeRequest
{
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
