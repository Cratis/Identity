// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>
/// Adds the response headers every Identity response carries.
/// </summary>
internal static class SecurityHeaders
{
    /// <summary>
    /// Adds the security headers middleware.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";

            // Authorization URLs carry state and PKCE challenges, and redirects carry codes; never leak them.
            headers["Referrer-Policy"] = "no-referrer";
            await next(context);
        });
}
