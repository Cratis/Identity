// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Realm;

/// <summary>Establishes the realm before endpoint dependencies and scoped stores are resolved.</summary>
internal sealed class IdentityRealmMiddleware
{
    readonly RequestDelegate _next;
    readonly PathString[] _prefixes;
    readonly bool _deviceEnabled;

    /// <summary>Initializes realm route matching from the same options as Arc.</summary>
    /// <param name="next">The next middleware.</param>
    /// <param name="options">Arc endpoint options.</param>
    /// <param name="identity">The opt-in device route configuration.</param>
    public IdentityRealmMiddleware(RequestDelegate next, IOptions<ArcOptions> options, IOptions<IdentityOptions>? identity = null)
    {
        _next = next;
        _deviceEnabled = identity?.Value.DeviceGrant.Enabled == true;
        var endpoints = options.Value.GeneratedApis;
        _prefixes = typeof(IdentityRealmMiddleware).Assembly.GetTypes()
            .Where(type => type.IsDefined(typeof(CommandAttribute)) || type.IsDefined(typeof(ReadModelAttribute)))
            .Select(type => new PathString(EndpointRouteHelper.BuildRouteUrl(endpoints, type.Namespace!.Split('.'), endpoints.SegmentsToSkipForRoute, string.Empty, false)))
            .Distinct().ToArray();
    }

    /// <summary>Runs an Identity endpoint inside the realm scope.</summary>
    /// <param name="context">The current request.</param>
    /// <returns>The middleware task.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var type = context.GetEndpoint()?.Metadata.GetMetadata<EndpointMetadata>()?.RequestBodyType;
        var path = context.Request.Path;
        var identity = type?.Namespace?.StartsWith("Cratis.Identity.", StringComparison.Ordinal) == true ||
            path.StartsWithSegments("/connect") || (_deviceEnabled && path == "/device") || path.StartsWithSegments("/.well-known") ||
            path.StartsWithSegments("/signin-github") || path.StartsWithSegments("/logout") ||
            _prefixes.Any(path.StartsWithSegments);
        if (!identity)
        {
            await _next(context);
            return;
        }

        using var realm = IdentityRealmScope.Begin();
        await _next(context);
    }
}
