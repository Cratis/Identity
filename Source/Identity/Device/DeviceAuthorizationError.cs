// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Device;

/// <summary>Returns protocol errors without an authentication forbid, which the device endpoint does not support.</summary>
/// <param name="error">The OAuth error.</param>
/// <param name="description">The optional error description.</param>
internal sealed class DeviceAuthorizationError(string error, string? description = null) : IResult
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = new Dictionary<string, string> { [Parameters.Error] = error };
        if (description is not null) response[Parameters.ErrorDescription] = description;
        var status = error switch
        {
            Errors.InvalidClient => StatusCodes.Status401Unauthorized,
            Errors.TemporarilyUnavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
        await Results.Json(response, statusCode: status).ExecuteAsync(httpContext);
    }
}
