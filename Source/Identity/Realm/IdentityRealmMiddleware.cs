// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Identity.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Realm;

/// <summary>Establishes the realm before endpoint dependencies and scoped stores are resolved.</summary>
internal sealed class IdentityRealmMiddleware
{
    readonly RequestDelegate _next;
    readonly PathString[] _prefixes;
    readonly IdentityEndpointOptions _endpoints;

    /// <summary>Initializes realm route matching from the same options as Arc.</summary>
    /// <param name="next">The next middleware.</param>
    /// <param name="options">Arc endpoint options.</param>
    /// <param name="identityEndpoints">Identity endpoint paths.</param>
    public IdentityRealmMiddleware(RequestDelegate next, IOptions<ArcOptions> options, IOptions<IdentityEndpointOptions>? identityEndpoints = null)
    {
        _next = next;
        _endpoints = identityEndpoints?.Value ?? new IdentityEndpointOptions();
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
        var identity = type?.Namespace?.StartsWith("Cratis.Identity.", StringComparison.Ordinal) == true || IsIdentityPath(context.Request.Path);
        if (!identity)
        {
            await _next(context);
            return;
        }

        using var realm = IdentityRealmScope.Begin();
        await _next(context);
    }

    bool IsIdentityPath(PathString path)
    {
        if (IdentityEndpointOptions.HasUnsafeSegments(path.Value ?? string.Empty))
        {
            return false;
        }

        if (path.StartsWithSegments("/.well-known") || path.StartsWithSegments("/signin-github"))
        {
            return true;
        }

        if (_endpoints.RoutePrefix.HasValue && !path.StartsWithSegments(_endpoints.RoutePrefix, out path))
        {
            return false;
        }

        return MatchesPrefixedRoute(path);
    }

    bool MatchesPrefixedRoute(PathString path) => path.StartsWithSegments("/connect") ||
        (_endpoints.MapSignOut && path.StartsWithSegments("/logout")) || _prefixes.Any(path.StartsWithSegments);
}
