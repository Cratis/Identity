// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Authorization.RateLimiting;

/// <summary>Limits token requests before realm processing and authentication.</summary>
/// <param name="next">The next middleware.</param>
/// <param name="endpoints">The endpoint options that define the token endpoint route prefix.</param>
internal sealed class TokenRateLimitMiddleware(RequestDelegate next, IOptions<IdentityEndpointOptions>? endpoints = null)
{
    readonly string _tokenEndpoint = $"{endpoints?.Value.RoutePrefix.Value}/connect/token".TrimStart('/');

    /// <summary>Admits a request or returns a non-cacheable OAuth refusal.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="limiter">The process-local admission limiter.</param>
    /// <param name="options">The Identity options.</param>
    /// <returns>The asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, TokenRequestRateLimiter limiter, IOptions<IdentityOptions> options)
    {
        if (!options.Value.TokenRateLimit.Enabled || !IsTokenRequest(context.Request, _tokenEndpoint))
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

    static bool IsTokenRequest(HttpRequest request, string tokenEndpoint)
    {
        // Match OpenIddict 7.8.0 ResolveRequestUri/InferEndpointType: canonicalize the
        // combined PathBase + Path with System.Uri, ignore query/case/trailing slash.
        var host = request.Host.HasValue ? request.Host : new HostString("localhost");
        var scheme = string.IsNullOrEmpty(request.Scheme) ? "http" : request.Scheme;
        if (!Uri.TryCreate(UriHelper.BuildAbsolute(scheme, host, request.PathBase), UriKind.Absolute, out var baseUri) ||
            !Uri.TryCreate(UriHelper.BuildAbsolute(scheme, host, request.PathBase, request.Path, request.QueryString), UriKind.Absolute, out var requestUri))
        {
            return true; // Do not let an unclassifiable URI bypass admission.
        }

        if (!baseUri.AbsolutePath.EndsWith('/')) baseUri = new UriBuilder(baseUri) { Path = baseUri.AbsolutePath + "/" }.Uri;
        var endpoint = new Uri(baseUri, tokenEndpoint);
        return string.Equals(endpoint.AbsolutePath.TrimEnd('/'), requestUri.AbsolutePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }
}
