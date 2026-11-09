// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Authorization.RateLimiting;

/// <summary>Limits token requests before realm processing and authentication.</summary>
/// <param name="next">The next middleware.</param>
internal sealed class TokenRateLimitMiddleware(RequestDelegate next)
{
    /// <summary>Admits a request or returns a non-cacheable OAuth refusal.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="limiter">The process-local admission limiter.</param>
    /// <param name="options">The Identity options.</param>
    /// <returns>The asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, TokenRequestRateLimiter limiter, IOptions<IdentityOptions> options)
    {
        if (!options.Value.TokenRateLimit.Enabled || !context.Request.Path.Equals("/connect/token", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var request = await TokenRequestIdentification.Read(context);
        var admission = limiter.Acquire(request.ClientId, request.GrantType, request.Source);
        if (admission.Allowed)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = admission.RetryAfter.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(/*lang=json*/ "{\"error\":\"temporarily_unavailable\",\"error_description\":\"Too many requests. Try again later.\"}", context.RequestAborted);
    }
}
